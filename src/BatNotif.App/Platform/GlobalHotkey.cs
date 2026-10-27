using System.Windows.Interop;

namespace BatNotif.App.Platform;

/// <summary>
/// Raccourci clavier global <c>Ctrl+Alt+B</c> (FR-031, research R8) : <c>RegisterHotKey</c> sur une fenêtre
/// de messages dédiée, invisible. Fonctionne même quand l'icône est cachée dans le tiroir de Windows 11.
/// </summary>
public sealed class GlobalHotkey : IDisposable
{
    private const int HotkeyId = 0xB47; // Identifiant arbitraire propre à BatNotif.
    private const uint VirtualKeyB = 0x42;
    private static readonly nint HwndMessage = -3;

    private readonly HwndSource _source;

    public GlobalHotkey()
    {
        _source = new HwndSource(new HwndSourceParameters("BatNotif.Hotkey")
        {
            ParentWindow = HwndMessage, // Fenêtre « message-only » : jamais affichée.
            Width = 0,
            Height = 0,
        });
        _source.AddHook(OnMessage);
        IsRegistered = NativeMethods.RegisterHotKey(
            _source.Handle,
            HotkeyId,
            NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT | NativeMethods.MOD_NOREPEAT,
            VirtualKeyB);
    }

    /// <summary>Faux si le raccourci est déjà pris par une autre application.</summary>
    public bool IsRegistered { get; }

    public const string Label = "Ctrl+Alt+B";

    /// <summary>Levé sur le thread UI à chaque appui sur le raccourci.</summary>
    public event EventHandler? Pressed;

    public void Dispose()
    {
        if (IsRegistered)
        {
            NativeMethods.UnregisterHotKey(_source.Handle, HotkeyId);
        }

        _source.RemoveHook(OnMessage);
        _source.Dispose();
    }

    private nint OnMessage(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY && wParam == HotkeyId)
        {
            Pressed?.Invoke(this, EventArgs.Empty);
            handled = true;
        }

        return 0;
    }
}
