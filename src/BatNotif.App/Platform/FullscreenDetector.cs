namespace BatNotif.App.Platform;

/// <summary>
/// Détecte une application en plein écran (research R5, FR-037) :
/// état de notification de Windows (jeu D3D, plein écran, présentation) ou fenêtre au premier plan
/// qui couvre tout son écran. Le bureau, la barre des tâches et les fenêtres de BatNotif sont ignorés.
/// </summary>
public static class FullscreenDetector
{
    private static readonly HashSet<string> IgnoredClasses = ["Progman", "WorkerW", "Shell_TrayWnd", "Shell_SecondaryTrayWnd"];

    public static bool IsFullscreenActive()
    {
        if (NativeMethods.SHQueryUserNotificationState(out var state) == 0
            && state is NativeMethods.UserNotificationState.RunningD3DFullScreen
                or NativeMethods.UserNotificationState.Busy
                or NativeMethods.UserNotificationState.PresentationMode)
        {
            return true;
        }

        return ForegroundCoversItsMonitor();
    }

    private static bool ForegroundCoversItsMonitor()
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == 0
            || hwnd == NativeMethods.GetShellWindow()
            || hwnd == NativeMethods.GetDesktopWindow()
            || IgnoredClasses.Contains(NativeMethods.ClassNameOf(hwnd))
            || IsOwnWindow(hwnd))
        {
            return false;
        }

        if (!NativeMethods.GetWindowRect(hwnd, out var window))
        {
            return false;
        }

        var monitor = NativeMethods.MonitorFromWindow(hwnd, NativeMethods.MONITOR_DEFAULTTONEAREST);
        var info = new NativeMethods.MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.MONITORINFO>() };
        if (!NativeMethods.GetMonitorInfo(monitor, ref info))
        {
            return false;
        }

        var screen = info.rcMonitor;
        return window.Left <= screen.Left && window.Top <= screen.Top
            && window.Right >= screen.Right && window.Bottom >= screen.Bottom;
    }

    private static bool IsOwnWindow(nint hwnd)
    {
        NativeMethods.GetWindowThreadProcessId(hwnd, out var pid);
        return pid == Environment.ProcessId;
    }
}
