using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows.Input;
using System.Windows.Threading;
using BatNotif.App.Audio;
using BatNotif.App.Platform;
using BatNotif.Core.Alerts;
using BatNotif.Core.Logging;
using BatNotif.Core.Settings;

namespace BatNotif.App.ViewModels;

/// <summary>
/// Fenêtre principale (ui-contract §3) : état en direct (FR-033) et réglages validés, enregistrés et appliqués
/// (FR-017 à FR-022).
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    private readonly AlertEngine _engine;
    private readonly SettingsStore _store;
    private readonly LoopingAlarmPlayer _player;
    private readonly AutostartRegistry _autostart;
    private readonly Func<string?> _pickSoundFile;
    private readonly Dispatcher _dispatcher;

    private AppSettings _saved;
    private AlertOutput _output;
    private string _lowThresholdText;
    private string _highThresholdText;
    private string _lowSoundPath;
    private string _highSoundPath;
    private double _volumePercent;
    private bool _startWithWindows;
    private string _validationMessage = "";
    private string _statusMessage = "";

    public MainViewModel(
        AlertEngine engine,
        SettingsStore store,
        LoopingAlarmPlayer player,
        AutostartRegistry autostart,
        AppSettings initial,
        IEnumerable<string> initialWarnings,
        Func<string?> pickSoundFile,
        Dispatcher dispatcher)
    {
        _engine = engine;
        _store = store;
        _player = player;
        _autostart = autostart;
        _pickSoundFile = pickSoundFile;
        _dispatcher = dispatcher;
        _saved = initial;
        _output = engine.Output;

        _lowThresholdText = initial.LowThreshold.ToString(CultureInfo.InvariantCulture);
        _highThresholdText = initial.HighThreshold.ToString(CultureInfo.InvariantCulture);
        _lowSoundPath = initial.LowSoundPath;
        _highSoundPath = initial.HighSoundPath;
        _volumePercent = Math.Round(initial.Volume * 100);
        _startWithWindows = initial.StartWithWindows;

        foreach (var warning in initialWarnings)
        {
            AddWarning(warning);
        }

        CheckSoundFiles(initial);
        Warnings.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasWarnings));

        SaveCommand = new RelayCommand(Save);
        ResetLowCommand = new RelayCommand(() => LowThresholdText = AlertConstants.DefaultLowThreshold.ToString(CultureInfo.InvariantCulture));
        ResetHighCommand = new RelayCommand(() => HighThresholdText = AlertConstants.DefaultHighThreshold.ToString(CultureInfo.InvariantCulture));
        ChooseLowSoundCommand = new RelayCommand(() => LowSoundPath = _pickSoundFile() ?? LowSoundPath);
        ChooseHighSoundCommand = new RelayCommand(() => HighSoundPath = _pickSoundFile() ?? HighSoundPath);
        UseBuiltInLowSoundCommand = new RelayCommand(() => LowSoundPath = "");
        UseBuiltInHighSoundCommand = new RelayCommand(() => HighSoundPath = "");
        TestLowSoundCommand = new RelayCommand(() => _player.PlayPreview(AlertSound.Low, LowSoundPath, (float)(VolumePercent / 100)));
        TestHighSoundCommand = new RelayCommand(() => _player.PlayPreview(AlertSound.High, HighSoundPath, (float)(VolumePercent / 100)));
        DismissWarningsCommand = new RelayCommand(Warnings.Clear);

        _engine.OutputChanged += (_, output) => _dispatcher.BeginInvoke(() => Output = output);
    }

    /// <summary>Levé après un enregistrement réussi, avec les réglages appliqués.</summary>
    public event EventHandler<AppSettings>? SettingsSaved;

    // --- État en direct (FR-033) ---

    public AlertOutput Output
    {
        get => _output;
        private set
        {
            if (Set(ref _output, value))
            {
                OnPropertyChanged(nameof(Level));
                OnPropertyChanged(nameof(LevelText));
                OnPropertyChanged(nameof(AcText));
                OnPropertyChanged(nameof(StateText));
                OnPropertyChanged(nameof(HasBattery));
            }
        }
    }

    public int Level => _output.Level;

    public string LevelText => HasBattery ? $"{_output.Level} %" : "—";

    public bool HasBattery => _output.State != AlertState.NoBattery;

    public string AcText => _output.AcPresent ? "Branché" : "Sur batterie";

    public string StateText => _output.State switch
    {
        AlertState.AlertLow => "Alerte basse",
        AlertState.AlertHigh => "Alerte haute",
        AlertState.NoBattery => "Aucune batterie détectée",
        _ => "Normal",
    };

    // --- Réglages (FR-017) ---

    public string LowThresholdText
    {
        get => _lowThresholdText;
        set => Set(ref _lowThresholdText, value);
    }

    public string HighThresholdText
    {
        get => _highThresholdText;
        set => Set(ref _highThresholdText, value);
    }

    public string LowSoundPath
    {
        get => _lowSoundPath;
        set
        {
            if (Set(ref _lowSoundPath, value))
            {
                OnPropertyChanged(nameof(LowSoundLabel));
            }
        }
    }

    public string HighSoundPath
    {
        get => _highSoundPath;
        set
        {
            if (Set(ref _highSoundPath, value))
            {
                OnPropertyChanged(nameof(HighSoundLabel));
            }
        }
    }

    public string LowSoundLabel => SoundLabel(_lowSoundPath);

    public string HighSoundLabel => SoundLabel(_highSoundPath);

    public double VolumePercent
    {
        get => _volumePercent;
        set
        {
            if (Set(ref _volumePercent, Math.Round(Math.Clamp(value, 0, 100))))
            {
                OnPropertyChanged(nameof(VolumeText));
            }
        }
    }

    public string VolumeText => $"{_volumePercent:0} %";

    public bool StartWithWindows
    {
        get => _startWithWindows;
        set => Set(ref _startWithWindows, value);
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => Set(ref _validationMessage, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => Set(ref _statusMessage, value);
    }

    /// <summary>Avertissements : réglages réinitialisés, son introuvable, erreur bloquante, raccourci indisponible.</summary>
    public ObservableCollection<string> Warnings { get; } = [];

    public bool HasWarnings => Warnings.Count > 0;

    public ICommand SaveCommand { get; }

    public ICommand ResetLowCommand { get; }

    public ICommand ResetHighCommand { get; }

    public ICommand ChooseLowSoundCommand { get; }

    public ICommand ChooseHighSoundCommand { get; }

    public ICommand UseBuiltInLowSoundCommand { get; }

    public ICommand UseBuiltInHighSoundCommand { get; }

    public ICommand TestLowSoundCommand { get; }

    public ICommand TestHighSoundCommand { get; }

    public ICommand DismissWarningsCommand { get; }

    /// <summary>Ajoute un avertissement (sans doublon). Appelable depuis n'importe quel thread.</summary>
    public void AddWarning(string message)
    {
        if (!_dispatcher.CheckAccess())
        {
            _dispatcher.BeginInvoke(() => AddWarning(message));
            return;
        }

        if (!Warnings.Contains(message))
        {
            Warnings.Add(message);
        }
    }

    private void Save()
    {
        StatusMessage = "";
        if (!int.TryParse(LowThresholdText, NumberStyles.Integer, CultureInfo.CurrentCulture, out var low)
            || !int.TryParse(HighThresholdText, NumberStyles.Integer, CultureInfo.CurrentCulture, out var high))
        {
            ValidationMessage = "Les seuils doivent être des nombres entiers.";
            return;
        }

        var candidate = _saved with
        {
            LowThreshold = low,
            HighThreshold = high,
            LowSoundPath = LowSoundPath.Trim(),
            HighSoundPath = HighSoundPath.Trim(),
            Volume = VolumePercent / 100,
            StartWithWindows = StartWithWindows,
        };

        var validation = SettingsValidator.Validate(candidate);
        if (!validation.IsValid)
        {
            ValidationMessage = string.Join(Environment.NewLine, validation.Errors);
            return;
        }

        try
        {
            _store.Save(candidate);
        }
        catch (Exception ex)
        {
            ValidationMessage = $"Enregistrement impossible : {ex.Message}";
            App.Log.Write(App.Time, LogEventKind.Error, $"Enregistrement des réglages impossible — {ex}");
            return;
        }

        ValidationMessage = "";
        _engine.UpdateThresholds(candidate.LowThreshold, candidate.HighThreshold);
        _player.Volume = (float)candidate.Volume;
        if (!_autostart.Apply(candidate.StartWithWindows))
        {
            AddWarning("Le démarrage automatique avec Windows n'a pas pu être modifié (voir le journal).");
        }

        CheckSoundFiles(candidate);
        _saved = candidate;
        StatusMessage = "Réglages enregistrés.";
        SettingsSaved?.Invoke(this, candidate);
    }

    private void CheckSoundFiles(AppSettings settings)
    {
        CheckSoundFile(settings.LowSoundPath, "basse");
        CheckSoundFile(settings.HighSoundPath, "haute");
    }

    private void CheckSoundFile(string path, string label)
    {
        if (!string.IsNullOrWhiteSpace(path) && !File.Exists(path))
        {
            AddWarning($"Le son de l'alerte {label} est introuvable ({Path.GetFileName(path)}) : le son intégré sera utilisé.");
        }
    }

    private static string SoundLabel(string path) =>
        string.IsNullOrWhiteSpace(path) ? "Son intégré" : Path.GetFileName(path);
}
