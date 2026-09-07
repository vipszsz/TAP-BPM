using System.Runtime.InteropServices;

namespace TapBpm.Core;

/// <summary>
/// A system-wide hotkey, so you can tap while your DAW keeps focus.
/// </summary>
/// <remarks>
/// Messages are received on a hidden native window rather than the form, so the hotkey
/// keeps working regardless of what the form is doing, and unregisters cleanly on dispose.
/// </remarks>
public sealed class GlobalHotkey : IDisposable
{
    private const int WM_HOTKEY = 0x0312;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [Flags]
    public enum Modifiers : uint
    {
        None = 0,
        Alt = 0x0001,
        Control = 0x0002,
        Shift = 0x0004,
        Win = 0x0008,
        NoRepeat = 0x4000,   // without this, holding the key machine-guns taps
    }

    private readonly MessageWindow _window;
    private readonly int _id;
    private bool _registered;

    public event EventHandler? Pressed;

    public GlobalHotkey(Modifiers modifiers, Keys key, int id = 1)
    {
        _id = id;
        _window = new MessageWindow(OnMessage);
        Modifier = modifiers;
        Key = key;
    }

    public Modifiers Modifier { get; private set; }
    public Keys Key { get; private set; }
    public bool IsRegistered => _registered;

    /// <summary>Registers the hotkey. Returns false if another application already owns it.</summary>
    public bool Register()
    {
        if (_registered)
            return true;

        _registered = RegisterHotKey(_window.Handle, _id, (uint)(Modifier | Modifiers.NoRepeat), (uint)Key);
        return _registered;
    }

    public void Unregister()
    {
        if (!_registered)
            return;

        UnregisterHotKey(_window.Handle, _id);
        _registered = false;
    }

    public bool Rebind(Modifiers modifiers, Keys key)
    {
        Unregister();
        Modifier = modifiers;
        Key = key;
        return Register();
    }

    /// <summary>
    /// Takes the first combination nothing else has claimed.
    /// </summary>
    /// <remarks>
    /// Windows allows only one owner per global hotkey, and popular combinations are often
    /// already taken by another app. Falling back through a short list means the feature
    /// still works instead of silently doing nothing.
    /// </remarks>
    public bool RegisterFirstAvailable(IEnumerable<(Modifiers Modifiers, Keys Key)> candidates)
    {
        foreach ((Modifiers modifiers, Keys key) in candidates)
            if (Rebind(modifiers, key))
                return true;

        return false;
    }

    /// <summary>The active combination in the form a user would recognise, e.g. "ctrl+alt+space".</summary>
    public string Describe()
    {
        var parts = new List<string>(4);
        if (Modifier.HasFlag(Modifiers.Control)) parts.Add("ctrl");
        if (Modifier.HasFlag(Modifiers.Alt)) parts.Add("alt");
        if (Modifier.HasFlag(Modifiers.Shift)) parts.Add("shift");
        if (Modifier.HasFlag(Modifiers.Win)) parts.Add("win");
        parts.Add(Key == Keys.Space ? "space" : Key.ToString().ToLowerInvariant());
        return string.Join("+", parts);
    }

    public void Dispose()
    {
        Unregister();
        _window.DestroyHandle();
    }

    private void OnMessage(ref Message m)
    {
        if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == _id)
            Pressed?.Invoke(this, EventArgs.Empty);
    }

    private sealed class MessageWindow : NativeWindow
    {
        private readonly RefAction _handler;

        internal delegate void RefAction(ref Message m);

        private const int HWND_MESSAGE = -3;

        internal MessageWindow(RefAction handler)
        {
            _handler = handler;
            // A message-only window: it never appears anywhere, not even as a zero-sized
            // entry in the window list.
            CreateHandle(new CreateParams { Parent = new IntPtr(HWND_MESSAGE) });
        }

        protected override void WndProc(ref Message m)
        {
            _handler(ref m);
            base.WndProc(ref m);
        }
    }
}
