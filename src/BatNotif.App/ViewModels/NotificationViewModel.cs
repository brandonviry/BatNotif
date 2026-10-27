using System.Windows.Media;
using BatNotif.App.Audio;

namespace BatNotif.App.ViewModels;

/// <summary>Mode d'affichage de la notification (ui-contract §4).</summary>
public enum NotificationMode
{
    /// <summary>Compacte, coin haut-droit, ne prend jamais le focus.</summary>
    Normal,

    /// <summary>Agrandie, centrée, passe devant une application plein écran (FR-037).</summary>
    Enhanced,
}

/// <summary>Contenu de la notification d'alerte (FR-012).</summary>
public sealed class NotificationViewModel : ObservableObject
{
    private static readonly Brush LowAccent = Frozen(Color.FromRgb(0xE5, 0x48, 0x4D));
    private static readonly Brush HighAccent = Frozen(Color.FromRgb(0xF5, 0xA5, 0x24));

    private AlertSound _sound;
    private int _level;
    private NotificationMode _mode;

    public AlertSound Sound
    {
        get => _sound;
        set
        {
            if (Set(ref _sound, value))
            {
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(Message));
                OnPropertyChanged(nameof(Glyph));
                OnPropertyChanged(nameof(Accent));
            }
        }
    }

    /// <summary>Niveau mis à jour en direct tant que la notification est visible (FR-014).</summary>
    public int Level
    {
        get => _level;
        set
        {
            if (Set(ref _level, value))
            {
                OnPropertyChanged(nameof(Message));
            }
        }
    }

    public NotificationMode Mode
    {
        get => _mode;
        set => Set(ref _mode, value);
    }

    public string Title => Sound == AlertSound.Low ? "Batterie faible" : "Batterie haute";

    public string Message => Sound == AlertSound.Low
        ? $"Batterie à {Level} % — branche le chargeur"
        : $"Batterie à {Level} % — débranche le chargeur";

    /// <summary>Glyphe Segoe : batterie vide (alerte basse) ou pleine (alerte haute).</summary>
    public string Glyph => Sound == AlertSound.Low ? "" : "";

    public Brush Accent => Sound == AlertSound.Low ? LowAccent : HighAccent;

    private static Brush Frozen(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
