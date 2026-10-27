using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using BatNotif.App.Platform;
using BatNotif.App.ViewModels;

namespace BatNotif.App.Views;

/// <summary>
/// Notification d'alerte maison (ui-contract §4) : sans bordure, toujours au premier plan, sans bouton de fermeture
/// ni fermeture automatique. Mode normal = coin haut-droit de l'écran actif, ne prend pas le focus ;
/// mode renforcé = 2× plus grande, centrée, peut prendre le focus (FR-011, FR-037).
/// </summary>
public partial class NotificationWindow : Window
{
    private const double NormalWidth = 360;
    private const double NormalHeight = 120;
    private const double EnhancedScale = 2;
    private const double ScreenMargin = 16;

    private static readonly nint HwndTopmost = -1;

    public NotificationWindow(NotificationViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = viewModel;
        SourceInitialized += (_, _) => ApplyExtendedStyle();
        DpiChanged += (_, _) => Dispatcher.BeginInvoke(PlaceOnActiveScreen, DispatcherPriority.Loaded);
    }

    public NotificationViewModel ViewModel { get; }

    private nint Handle => new WindowInteropHelper(this).Handle;

    /// <summary>Affiche la fenêtre sans l'activer, puis la place sur l'écran actif.</summary>
    public void Present()
    {
        if (!IsVisible)
        {
            Show();
        }

        Dispatcher.BeginInvoke(PlaceOnActiveScreen, DispatcherPriority.Loaded);
    }

    /// <summary>Change de mode : taille, style (activable ou non) et position.</summary>
    public void SetMode(NotificationMode mode)
    {
        ViewModel.Mode = mode;
        var scale = mode == NotificationMode.Enhanced ? EnhancedScale : 1;
        Width = NormalWidth * scale;
        Height = NormalHeight * scale;
        ApplyExtendedStyle();
        Dispatcher.BeginInvoke(PlaceOnActiveScreen, DispatcherPriority.Loaded);
    }

    /// <summary>Remet la fenêtre au-dessus des autres fenêtres « toujours au premier plan ».</summary>
    public void ReassertTopmost()
    {
        if (Handle != 0)
        {
            NativeMethods.SetWindowPos(Handle, HwndTopmost, 0, 0, 0, 0,
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
        }
    }

    private void ApplyExtendedStyle()
    {
        var hwnd = Handle;
        if (hwnd == 0)
        {
            return;
        }

        var style = (long)NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE);
        style |= NativeMethods.WS_EX_TOOLWINDOW; // Absente d'Alt+Tab et de la barre des tâches.
        style = ViewModel.Mode == NotificationMode.Normal
            ? style | NativeMethods.WS_EX_NOACTIVATE
            : style & ~(long)NativeMethods.WS_EX_NOACTIVATE;
        NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE, (nint)style);
        NativeMethods.SetWindowPos(hwnd, HwndTopmost, 0, 0, 0, 0,
            NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_FRAMECHANGED);
    }

    /// <summary>Positionne en pixels physiques : fiable quel que soit le facteur d'échelle de chaque écran.</summary>
    private void PlaceOnActiveScreen()
    {
        var hwnd = Handle;
        if (hwnd == 0 || !NativeMethods.GetWindowRect(hwnd, out var self))
        {
            return;
        }

        var dpiScale = VisualTreeHelper.GetDpi(this).DpiScaleX;
        var margin = (int)(ScreenMargin * dpiScale);
        int x, y;
        if (ViewModel.Mode == NotificationMode.Enhanced)
        {
            var screen = ScreenHelper.ActiveMonitorBounds();
            x = screen.Left + (screen.Width - self.Width) / 2;
            y = screen.Top + (screen.Height - self.Height) / 2;
        }
        else
        {
            var work = ScreenHelper.ActiveWorkArea();
            x = work.Right - self.Width - margin;
            y = work.Top + margin;
        }

        NativeMethods.SetWindowPos(hwnd, HwndTopmost, x, y, 0, 0, NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
    }
}
