using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Controls;
using BatNotif.App.Platform;
using H.NotifyIcon;

namespace BatNotif.App.Tray;

/// <summary>Actions proposées par le menu de l'icône.</summary>
public sealed record TrayActions(
    Action Open,
    Action Pause15,
    Action Pause60,
    Action PauseIndefinite,
    Action Resume,
    Action Quit);

/// <summary>
/// Icône de zone de notification. Icône statique générée par le code ; menu Ouvrir / Pause… / Reprendre / Quitter
/// (ui-contract §1). Complétée en US4 (icônes d'état, clic gauche).
/// </summary>
public sealed class TrayIconService : IDisposable
{
    private readonly TaskbarIcon _icon;
    private readonly Icon _image;
    private readonly MenuItem[] _pauseItems;
    private readonly MenuItem _resumeItem;

    public TrayIconService(TrayActions actions)
    {
        _image = CreateIcon();

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
        _resumeItem.Visibility = System.Windows.Visibility.Collapsed;
        menu.Items.Add(_resumeItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(Item("Quitter", actions.Quit));

        _icon = new TaskbarIcon
        {
            ToolTipText = "BatNotif",
            Icon = _image,
            ContextMenu = menu,
        };
        _icon.ForceCreate(enablesEfficiencyMode: false);
    }

    /// <summary>Pause en cours : affiche « Reprendre » à la place des entrées de pause (thread UI).</summary>
    public void SetPaused(bool paused)
    {
        foreach (var item in _pauseItems)
        {
            item.Visibility = paused ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
        }

        _resumeItem.Visibility = paused ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
    }

    public void Dispose()
    {
        _icon.Dispose();
        _image.Dispose();
    }

    private static MenuItem Item(string header, Action action, bool bold = false)
    {
        var item = new MenuItem { Header = header };
        if (bold)
        {
            item.FontWeight = System.Windows.FontWeights.SemiBold;
        }

        item.Click += (_, _) => action();
        return item;
    }

    /// <summary>Pastille verte « B » 32×32, sans fichier externe (research R7).</summary>
    private static Icon CreateIcon()
    {
        using var bitmap = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.Transparent);
            using var fill = new SolidBrush(Color.FromArgb(0x2E, 0x9E, 0x5B));
            g.FillEllipse(fill, 1, 1, 30, 30);
            using var font = new Font("Segoe UI", 15, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel);
            using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("B", font, Brushes.White, new RectangleF(0, 0, 32, 33), format);
        }

        var handle = bitmap.GetHicon();
        try
        {
            using var temporary = Icon.FromHandle(handle);
            return (Icon)temporary.Clone();
        }
        finally
        {
            NativeMethods.DestroyIcon(handle);
        }
    }
}
