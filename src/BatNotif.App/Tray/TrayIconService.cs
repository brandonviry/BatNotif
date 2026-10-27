using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using BatNotif.App.ViewModels;
using BatNotif.Core.Alerts;
using H.NotifyIcon;

namespace BatNotif.App.Tray;

/// <summary>Actions proposées par l'icône et son menu.</summary>
public sealed record TrayActions(
    Action ToggleWindow,
    Action Open,
    Action Pause15,
    Action Pause60,
    Action PauseIndefinite,
    Action Resume,
    Action Quit);

/// <summary>
/// Icône de zone de notification (ui-contract §1, FR-028, FR-029) : pastille qui reflète l'état et le niveau,
/// info-bulle détaillée, clic gauche = afficher / masquer la fenêtre, clic droit = Ouvrir / Pause… / Reprendre / Quitter.
/// À utiliser depuis le thread UI.
/// </summary>
public sealed class TrayIconService : IDisposable
{
    private readonly TaskbarIcon _icon;
    private readonly MenuItem[] _pauseItems;
    private readonly MenuItem _resumeItem;
    private Icon? _image;
    private (Color Background, string Text)? _imageKey;

    public TrayIconService(TrayActions actions, AlertOutput initial)
    {
        var menu = new ContextMenu();
        menu.Items.Add(Item("Ouvrir", actions.Open, bold: true));
        menu.Items.Add(new Separator());
        _pauseItems =
        [
            Item("Pause 15 min", actions.Pause15),
            Item("Pause 1 h", actions.Pause60),
            Item("Pause indéfinie", actions.PauseIndefinite),
        ];
        foreach (var item in _pauseItems)
        {
            menu.Items.Add(item);
        }

        _resumeItem = Item("Reprendre", actions.Resume, bold: true);
        menu.Items.Add(_resumeItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(Item("Quitter", actions.Quit));

        _icon = new TaskbarIcon
        {
            ContextMenu = menu,
            NoLeftClickDelay = true, // Réaction immédiate au clic gauche (pas d'attente du double-clic).
            LeftClickCommand = new RelayCommand(actions.ToggleWindow),
        };
        Update(initial);
        _icon.ForceCreate(enablesEfficiencyMode: false);
    }

    /// <summary>Met à jour l'icône, l'info-bulle et le menu selon la sortie du moteur.</summary>
    public void Update(AlertOutput output)
    {
        var key = TrayIconFactory.KeyFor(output);
        if (key != _imageKey)
        {
            var previous = _image;
            _image = TrayIconFactory.Create(key);
            _imageKey = key;
            _icon.Icon = _image;
            previous?.Dispose();
        }

        _icon.ToolTipText = ToolTipFor(output);

        var paused = output.IsPaused;
        foreach (var item in _pauseItems)
        {
            item.Visibility = paused ? Visibility.Collapsed : Visibility.Visible;
        }

        _resumeItem.Visibility = paused ? Visibility.Visible : Visibility.Collapsed;
    }

    public void Dispose()
    {
        _icon.Dispose();
        _image?.Dispose();
    }

    /// <summary>Exemple : « BatNotif — 74 % · branché · normal ».</summary>
    public static string ToolTipFor(AlertOutput output)
    {
        if (output.State == AlertState.NoBattery)
        {
            return "BatNotif — aucune batterie détectée";
        }

        var state = output switch
        {
            { PauseIndefinite: true } => "pause indéfinie",
            { PauseUntil: { } until } => $"pause jusqu'à {until.ToLocalTime():HH:mm}",
            { SnoozeUntil: { } until } => $"snooze jusqu'à {until.ToLocalTime():HH:mm}",
            { State: AlertState.AlertLow } => "alerte basse",
            { State: AlertState.AlertHigh } => "alerte haute",
            _ => "normal",
        };
        return $"BatNotif — {output.Level} % · {(output.AcPresent ? "branché" : "sur batterie")} · {state}";
    }

    private static MenuItem Item(string header, Action action, bool bold = false)
    {
        var item = new MenuItem { Header = header };
        if (bold)
        {
            item.FontWeight = FontWeights.SemiBold;
        }

        item.Click += (_, _) => action();
        return item;
    }
}
