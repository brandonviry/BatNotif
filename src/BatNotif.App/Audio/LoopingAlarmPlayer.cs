using System.IO;
using BatNotif.Core.Logging;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace BatNotif.App.Audio;

/// <summary>
/// Joue le son d'une alerte en boucle continue au volume réglé (FR-015, research R6).
/// Chemin vide → son intégré ; fichier introuvable ou illisible → son intégré + <see cref="SoundFallback"/> (FR-016).
/// À utiliser depuis le thread UI.
/// </summary>
public sealed class LoopingAlarmPlayer : IDisposable
{
    private readonly IEventLog _log;
    private readonly TimeProvider _time;
    private WaveOut? _output;
    private IDisposable? _reader;
    private VolumeSampleProvider? _volumeStage;
    private WaveOut? _previewOutput;
    private IDisposable? _previewReader;
    private float _volume = 0.8f;

    public LoopingAlarmPlayer(IEventLog log, TimeProvider time)
    {
        _log = log;
        _time = time;
    }

    /// <summary>Levé quand un fichier choisi ne peut pas être lu ; le message est destiné à l'utilisateur.</summary>
    public event EventHandler<string>? SoundFallback;

    public bool IsPlaying => _output?.PlaybackState == PlaybackState.Playing;

    public AlertSound? CurrentSound { get; private set; }

    /// <summary>Volume 0.0–1.0, appliqué aussi à la lecture en cours.</summary>
    public float Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, 0f, 1f);
            if (_volumeStage is not null)
            {
                _volumeStage.Volume = _volume;
            }
        }
    }

    /// <summary>Démarre (ou remplace) la lecture en boucle du son d'alerte.</summary>
    public void Play(AlertSound sound, string? path)
    {
        Stop();
        var (provider, reader) = OpenLooping(sound, path);
        _reader = reader;
        _volumeStage = new VolumeSampleProvider(provider) { Volume = _volume };
        _output = new WaveOut();
        _output.Init(_volumeStage);
        _output.Play();
        CurrentSound = sound;
    }

    public void Stop()
    {
        _output?.Stop();
        _output?.Dispose();
        _output = null;
        _reader?.Dispose();
        _reader = null;
        _volumeStage = null;
        CurrentSound = null;
    }

    /// <summary>Joue le son une seule fois (bouton « Tester », FR-020).</summary>
    public void PlayPreview(AlertSound sound, string? path, float volume)
    {
        StopPreview();
        var (provider, reader) = OpenOnce(sound, path);
        _previewReader = reader;
        _previewOutput = new WaveOut();
        _previewOutput.Init(new VolumeSampleProvider(provider) { Volume = Math.Clamp(volume, 0f, 1f) });
        _previewOutput.Play();
    }

    public void StopPreview()
    {
        _previewOutput?.Stop();
        _previewOutput?.Dispose();
        _previewOutput = null;
        _previewReader?.Dispose();
        _previewReader = null;
    }

    public void Dispose()
    {
        Stop();
        StopPreview();
    }

    private (ISampleProvider Provider, IDisposable? Reader) OpenLooping(AlertSound sound, string? path)
    {
        if (TryOpenFile(sound, path, out var file))
        {
            return (new LoopingSampleProvider(file, () => file.Position = 0), file);
        }

        var memory = new MemorySampleProvider(BuiltInSounds.For(sound), BuiltInSounds.SampleRate);
        return (new LoopingSampleProvider(memory, memory.Rewind), null);
    }

    private (ISampleProvider Provider, IDisposable? Reader) OpenOnce(AlertSound sound, string? path)
    {
        if (TryOpenFile(sound, path, out var file))
        {
            return (file, file);
        }

        return (new MemorySampleProvider(BuiltInSounds.For(sound), BuiltInSounds.SampleRate), null);
    }

    private bool TryOpenFile(AlertSound sound, string? path, out AudioFileReader file)
    {
        file = null!;
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Fichier introuvable", path);
            }

            file = new AudioFileReader(path);
            return true;
        }
        catch (Exception ex)
        {
            var label = sound == AlertSound.Low ? "basse" : "haute";
            var message = $"Le son de l'alerte {label} est introuvable ou illisible ({Path.GetFileName(path)}) : son intégré utilisé.";
            _log.Write(_time, LogEventKind.SoundFallback, $"{message} — {ex.Message}");
            SoundFallback?.Invoke(this, message);
            return false;
        }
    }
}
