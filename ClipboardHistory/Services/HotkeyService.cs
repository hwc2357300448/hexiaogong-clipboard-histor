using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Input;

namespace ClipboardHistory.Services
{
    public class HotkeyService : IDisposable
    {
        private const int WM_HOTKEY = 0x0312;
        private readonly int _hotkeyId = 9000;
        private IntPtr _windowHandle;
        private bool _isRegistered;

        public event Action? HotkeyPressed;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public bool RegisterHotkey(IntPtr windowHandle, string hotkey = "Ctrl+Shift+V")
        {
            _windowHandle = windowHandle;
            
            if (_isRegistered)
            {
                UnregisterHotkey();
            }

            var (modifiers, key) = ParseHotkey(hotkey);
            _isRegistered = RegisterHotKey(windowHandle, _hotkeyId, modifiers, (uint)key);
            
            return _isRegistered;
        }

        public void UnregisterHotkey()
        {
            if (_isRegistered && _windowHandle != IntPtr.Zero)
            {
                UnregisterHotKey(_windowHandle, _hotkeyId);
                _isRegistered = false;
            }
        }

        public bool ProcessHotkeyMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == _hotkeyId)
            {
                HotkeyPressed?.Invoke();
                return true;
            }
            return false;
        }

        private static (uint modifiers, Keys key) ParseHotkey(string hotkey)
        {
            uint modifiers = 0;
            var parts = hotkey.Split('+');
            var keyString = parts[^1];

            foreach (var part in parts[..^1])
            {
                modifiers |= part.Trim().ToLower() switch
                {
                    "ctrl" or "control" => 0x0002, // MOD_CONTROL
                    "shift" => 0x0004,              // MOD_SHIFT
                    "alt" => 0x0001,                // MOD_ALT
                    "win" or "windows" => 0x0008,   // MOD_WIN
                    _ => 0
                };
            }

            var key = keyString.Trim().ToUpper() switch
            {
                "V" => Keys.V,
                "C" => Keys.C,
                "X" => Keys.X,
                "Z" => Keys.Z,
                "F1" => Keys.F1,
                "F2" => Keys.F2,
                "F3" => Keys.F3,
                "F4" => Keys.F4,
                "F5" => Keys.F5,
                "F6" => Keys.F6,
                "F7" => Keys.F7,
                "F8" => Keys.F8,
                "F9" => Keys.F9,
                "F10" => Keys.F10,
                "F11" => Keys.F11,
                "F12" => Keys.F12,
                "SPACE" => Keys.Space,
                "ENTER" => Keys.Enter,
                "TAB" => Keys.Tab,
                "ESC" => Keys.Escape,
                _ => Keys.V
            };

            return (modifiers, key);
        }

        public void Dispose()
        {
            UnregisterHotkey();
        }
    }
}