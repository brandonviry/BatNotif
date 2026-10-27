using System.Runtime.InteropServices;

namespace BatNotif.App.Platform;

/// <summary>Écran « actif » : celui qui contient la fenêtre au premier plan (edge case multi-écrans).</summary>
internal static class ScreenHelper
{
    /// <summary>Zone de travail (hors barre des tâches) de l'écran actif, en pixels physiques.</summary>
    public static NativeMethods.RECT ActiveWorkArea() => ActiveMonitorInfo().rcWork;

    /// <summary>Surface complète de l'écran actif, en pixels physiques.</summary>
    public static NativeMethods.RECT ActiveMonitorBounds() => ActiveMonitorInfo().rcMonitor;

    private static NativeMethods.MONITORINFO ActiveMonitorInfo()
    {
        var foreground = NativeMethods.GetForegroundWindow();
        var monitor = NativeMethods.MonitorFromWindow(foreground, NativeMethods.MONITOR_DEFAULTTONEAREST);
        var info = new NativeMethods.MONITORINFO { cbSize = Marshal.SizeOf<NativeMethods.MONITORINFO>() };
        NativeMethods.GetMonitorInfo(monitor, ref info);
        return info;
    }
}
