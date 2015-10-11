using System;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace Auremo
{
    public partial class MainWindow
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr window, int id, int modifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr window, int id);

        private const int WM_HOTKEY = 0x0312;
        private const int VK_VOLUME_DOWN = 0xAE;
        private const int VK_VOLUME_UP = 0xAF;
        private const int VK_MEDIA_NEXT_TRACK = 0xB0;
        private const int VK_MEDIA_PREV_TRACK = 0xB1;
        private const int VK_MEDIA_STOP = 0xB2;
        private const int VK_MEDIA_PLAY_PAUSE = 0xB3;

        private void RegisterGlobalMediaKeys()
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;

            RegisterHotKey(handle, VK_VOLUME_DOWN, 0, (int)Key.VolumeDown);
            RegisterHotKey(handle, VK_VOLUME_UP, 0, (int)Key.VolumeUp);
            RegisterHotKey(handle, VK_MEDIA_NEXT_TRACK, 0, (int)Key.MediaNextTrack);
            RegisterHotKey(handle, VK_MEDIA_PREV_TRACK, 0, (int)Key.MediaPreviousTrack);
            RegisterHotKey(handle, VK_MEDIA_STOP, 0, (int)Key.MediaStop);
            RegisterHotKey(handle, VK_MEDIA_PLAY_PAUSE, 0, (int)Key.MediaPlayPause);

            ComponentDispatcher.ThreadPreprocessMessage += ProcessKeyPress;
        }

        private void UnregisterGlobalMediaKeys()
        {
            ComponentDispatcher.ThreadPreprocessMessage -= ProcessKeyPress;
            IntPtr handle = new WindowInteropHelper(this).Handle;

            UnregisterHotKey(handle, VK_VOLUME_DOWN);
            UnregisterHotKey(handle, VK_VOLUME_UP);
            UnregisterHotKey(handle, VK_MEDIA_NEXT_TRACK);
            UnregisterHotKey(handle, VK_MEDIA_PREV_TRACK);
            UnregisterHotKey(handle, VK_MEDIA_STOP);
            UnregisterHotKey(handle, VK_MEDIA_PLAY_PAUSE);
        }

        void ProcessKeyPress(ref MSG msg, ref bool handled)
        {
            if (msg.message == WM_HOTKEY)
            {
                Key key = (Key)msg.wParam;
                handled = HandleMediaKeys(key);
            }
        }
    }
}
