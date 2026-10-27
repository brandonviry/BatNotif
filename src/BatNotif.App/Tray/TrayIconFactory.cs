using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using BatNotif.App.Platform;
using BatNotif.Core.Alerts;

namespace BatNotif.App.Tray;

/// <summary>
/// Icônes de zone de notification générées par le code (FR-028, research R7) : une pastille dont la couleur
/// indique l'état et qui affiche le niveau. Aucun fichier .ico externe.
/// </summary>
public static class TrayIconFactory
{
    private const int Size = 32;

    private static readonly Color Normal = Color.FromArgb(0x2E, 0x9E, 0x5B);
    private static readonly Color Low = Color.FromArgb(0xE5, 0x48, 0x4D);
    private static readonly Color High = Color.FromArgb(0xF5, 0xA5, 0x24);
    private static readonly Color Paused = Color.FromArgb(0x8A, 0x8A, 0x93);
    private static readonly Color NoBattery = Color.FromArgb(0x5A, 0x5A, 0x66);

    /// <summary>Clé visuelle d'une icône : deux sorties qui donnent la même clé partagent la même image.</summary>
    public static (Color Background, string Text) KeyFor(AlertOutput output)
    {
        if (output.State == AlertState.NoBattery)
        {
            return (NoBattery, "—");
        }

        var background = output switch
        {
            { IsPaused: true } => Paused,
            { State: AlertState.AlertLow } => Low,
            { State: AlertState.AlertHigh } => High,
            _ => Normal,
        };
        return (background, output.Level.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>Dessine l'icône ; l'appelant en devient propriétaire et doit la libérer.</summary>
    public static Icon Create((Color Background, string Text) key)
    {
        using var bitmap = new Bitmap(Size, Size);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.Transparent);
            using var fill = new SolidBrush(key.Background);
            g.FillEllipse(fill, 0, 0, Size - 1, Size - 1);

            // Plus de chiffres = police plus petite, pour que « 100 » tienne dans la pastille.
            var fontSize = key.Text.Length switch
            {
                >= 3 => 12f,
                2 => 16f,
                _ => 19f,
            };
            using var font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
            using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(key.Text, font, Brushes.White, new RectangleF(0, 0, Size, Size + 1), format);
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
