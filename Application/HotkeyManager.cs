using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Input;

namespace Keymeleon
{
    internal class HotkeyManager
    {
        static class NativeMethods
        {
            [DllImport("user32.dll")]
            public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

            [DllImport("user32.dll")]
            public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        }

        private int key;
        private IntPtr hWnd;
        private int id;

        public HotkeyManager(Keys key, IntPtr hWnd)
        {
            this.key = (int) KeyInterop.KeyFromVirtualKey((int)key);
            this.hWnd = hWnd;
            id = this.key ^ hWnd.ToInt32();
        }

        public bool Register()
        {
            const uint MOD_CTRL = 0x0002;
            int a = (int) Keys.ControlKey;
            return NativeMethods.RegisterHotKey(hWnd, 9000, MOD_CTRL, 0);
        }

        public bool Unregiser()
        {
            return NativeMethods.UnregisterHotKey(hWnd, id);
        }
    }
}
