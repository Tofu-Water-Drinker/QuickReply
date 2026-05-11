using System.Runtime.InteropServices;

namespace QuickReply;

public sealed class HotkeyManager : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID = 0xB001;

    [Flags]
    private enum HotkeyModifiers : uint
    {
        Alt = 0x0001,
        Control = 0x0002,
        Shift = 0x0004,
        Win = 0x0008,
        NoRepeat = 0x4000
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly MessageWindow _window;
    private bool _registered;
    private string _currentHotkey = string.Empty;

    public event EventHandler? HotkeyPressed;

    public string CurrentHotkeyDisplay => _currentHotkey;

    public HotkeyManager()
    {
        _window = new MessageWindow(this);
    }

    public bool Register(string hotkey)
    {
        UnregisterIfNeeded();

        if (!ParseHotkey(hotkey, out var modifiers, out var vk, out var display))
        {
            return false;
        }

        modifiers |= (uint)HotkeyModifiers.NoRepeat;
        var ok = RegisterHotKey(_window.Handle, HOTKEY_ID, modifiers, vk);
        if (ok)
        {
            _registered = true;
            _currentHotkey = display;
        }
        return ok;
    }

    public void UnregisterIfNeeded()
    {
        if (_registered)
        {
            UnregisterHotKey(_window.Handle, HOTKEY_ID);
            _registered = false;
        }
    }

    public void Dispose()
    {
        UnregisterIfNeeded();
        _window.DestroyHandle();
    }

    private void OnHotkey()
    {
        HotkeyPressed?.Invoke(this, EventArgs.Empty);
    }

    public static bool ParseHotkey(string hotkey, out uint modifiers, out uint virtualKey, out string display)
    {
        modifiers = 0;
        virtualKey = 0;
        display = string.Empty;

        if (string.IsNullOrWhiteSpace(hotkey)) return false;

        var parts = hotkey.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return false;

        var modParts = new List<string>();
        string keyPart = string.Empty;

        foreach (var raw in parts)
        {
            var p = raw.ToLowerInvariant();
            switch (p)
            {
                case "ctrl":
                case "control":
                    modifiers |= (uint)HotkeyModifiers.Control;
                    modParts.Add("Ctrl");
                    break;
                case "alt":
                    modifiers |= (uint)HotkeyModifiers.Alt;
                    modParts.Add("Alt");
                    break;
                case "shift":
                    modifiers |= (uint)HotkeyModifiers.Shift;
                    modParts.Add("Shift");
                    break;
                case "win":
                case "windows":
                    modifiers |= (uint)HotkeyModifiers.Win;
                    modParts.Add("Win");
                    break;
                default:
                    keyPart = raw;
                    break;
            }
        }

        if (string.IsNullOrEmpty(keyPart)) return false;

        if (!TryParseKey(keyPart, out virtualKey, out var keyDisplay)) return false;

        modParts.Add(keyDisplay);
        display = string.Join("+", modParts);
        return true;
    }

    private static bool TryParseKey(string key, out uint vk, out string display)
    {
        vk = 0;
        display = key;

        var trimmed = key.Trim();
        if (trimmed.Length == 1)
        {
            var ch = trimmed[0];
            if (ch >= 'a' && ch <= 'z') { vk = (uint)char.ToUpperInvariant(ch); display = ch.ToString().ToUpper(); return true; }
            if (ch >= 'A' && ch <= 'Z') { vk = ch; display = ch.ToString(); return true; }
            if (ch >= '0' && ch <= '9') { vk = ch; display = ch.ToString(); return true; }

            switch (ch)
            {
                case ';': case ':': vk = 0xBA; display = ";"; return true;
                case '=': case '+': vk = 0xBB; display = "="; return true;
                case ',': vk = 0xBC; display = ","; return true;
                case '-': case '_': vk = 0xBD; display = "-"; return true;
                case '.': vk = 0xBE; display = "."; return true;
                case '/': case '?': vk = 0xBF; display = "/"; return true;
                case '`': case '~': vk = 0xC0; display = "`"; return true;
                case '[': case '{': vk = 0xDB; display = "["; return true;
                case '\\': case '|': vk = 0xDC; display = "\\"; return true;
                case ']': case '}': vk = 0xDD; display = "]"; return true;
                case '\'': case '"': vk = 0xDE; display = "'"; return true;
            }
        }

        var named = trimmed.ToLowerInvariant();
        switch (named)
        {
            case "semicolon": vk = 0xBA; display = ";"; return true;
            case "space": vk = 0x20; display = "Space"; return true;
            case "tab": vk = 0x09; display = "Tab"; return true;
            case "enter": case "return": vk = 0x0D; display = "Enter"; return true;
            case "escape": case "esc": vk = 0x1B; display = "Esc"; return true;
            case "backspace": vk = 0x08; display = "Backspace"; return true;
            case "f1": vk = 0x70; display = "F1"; return true;
            case "f2": vk = 0x71; display = "F2"; return true;
            case "f3": vk = 0x72; display = "F3"; return true;
            case "f4": vk = 0x73; display = "F4"; return true;
            case "f5": vk = 0x74; display = "F5"; return true;
            case "f6": vk = 0x75; display = "F6"; return true;
            case "f7": vk = 0x76; display = "F7"; return true;
            case "f8": vk = 0x77; display = "F8"; return true;
            case "f9": vk = 0x78; display = "F9"; return true;
            case "f10": vk = 0x79; display = "F10"; return true;
            case "f11": vk = 0x7A; display = "F11"; return true;
            case "f12": vk = 0x7B; display = "F12"; return true;
        }

        return false;
    }

    private sealed class MessageWindow : NativeWindow
    {
        private readonly HotkeyManager _owner;

        public MessageWindow(HotkeyManager owner)
        {
            _owner = owner;
            var cp = new CreateParams
            {
                Caption = "QuickReplyHotkeyWindow",
                Parent = new IntPtr(-3) // HWND_MESSAGE
            };
            CreateHandle(cp);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                _owner.OnHotkey();
                return;
            }
            base.WndProc(ref m);
        }
    }
}
