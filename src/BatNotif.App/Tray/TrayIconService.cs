using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Controls;
using BatNotif.App.Platform;
using H.NotifyIcon;

namespace BatNotif.App.Tray;

/// <summary>
/// Icône de zone de notification. Version minimale (US1) : icône statique générée par le code
/// et une seule entrée de menu « Quitter ». Complétée en US2 à US4.
/// </summary>
public sealed class TrayIconService : IDisposable
{
    private readonly TaskbarIcon _icon;
    private readonly Icon _image;

    public TrayIconService(Action quit)
    {
        _image = CreateIcon();

        var menu = new ContextMenu();
        var quitItem = new MenuItem { Header = "Quitter" };
        quitItem.Click += (_, _) => quit();
        menu.Items.Add(quitItem);

        _icon = new TaskbarIcon
        {
            ToolTipText = "BatNotif",
            Icon = _image,
            ContextMenu = menu,
        };
        _icon.ForceCreate(enablesEfficiencyMode: false);
    }

    public void Dispose()
    {
        _icon.Dispose();
        _image.Dispose();
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
