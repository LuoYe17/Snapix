using System;
using System.Windows.Forms;
using Snapix.Native;

namespace Snapix
{
    /// <summary>
    /// 管理全局热键注册/注销。
    /// </summary>
    internal sealed class HotkeyManager : IDisposable
    {
        private const int HOTKEY_ID = 9001;
        private readonly IntPtr _hwnd;
        private bool _registered;

        public HotkeyManager(IntPtr hwnd)
        {
            _hwnd = hwnd;
        }

        public bool Register(Keys key, uint modifiers = 0)
        {
            Unregister();
            _registered = NativeMethods.RegisterHotKey(_hwnd, HOTKEY_ID, modifiers, (uint)key);
            return _registered;
        }

        public void Unregister()
        {
            if (_registered)
            {
                NativeMethods.UnregisterHotKey(_hwnd, HOTKEY_ID);
                _registered = false;
            }
        }

        public void Dispose()
        {
            Unregister();
        }
    }
}
