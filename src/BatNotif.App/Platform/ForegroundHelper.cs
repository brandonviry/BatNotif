using System.Windows;
using System.Windows.Interop;

namespace BatNotif.App.Platform;

/// <summary>
/// Force une fenêtre au premier plan (mode renforcé, FR-037). Windows refuse normalement
/// SetForegroundWindow à une application en arrière-plan ; simuler une pression de la touche Alt
/// juste avant lève cette restriction (research R5).
/// </summary>
public static class ForegroundHelper
{
    /// <returns>Vrai si la fenêtre est bien au premier plan après l'appel.</returns>
    public static bool ForceForeground(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == 0)
        {
            return false;
        }

        NativeMethods.keybd_event(NativeMethods.VK_MENU, 0, 0, 0);
        NativeMethods.keybd_event(NativeMethods.VK_MENU, 0, NativeMethods.KEYEVENTF_KEYUP, 0);
        NativeMethods.SetForegroundWindow(hwnd);
        window.Activate();

        return NativeMethods.GetForegroundWindow() == hwnd;
    }
}
