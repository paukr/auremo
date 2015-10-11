using System;
using System.Runtime.InteropServices;
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

        private const int MediaKeyVolumeUp = 1;
        private const int MediaKeyVolumeDown = 2;
        private const int MediaKeyNext = 3;
        private const int MediaKeyPrevious = 4;
        private const int MediaKeyStop = 5;
        private const int MediaKeyPlayPause = 6;

        private void RegisterGlobalMediaKeys()
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;

            RegisterHotKey(handle, VK_VOLUME_DOWN, 0, MediaKeyVolumeDown);
            RegisterHotKey(handle, VK_VOLUME_UP, 0, MediaKeyVolumeUp);
            RegisterHotKey(handle, VK_MEDIA_NEXT_TRACK, 0, MediaKeyNext);
            RegisterHotKey(handle, VK_MEDIA_PREV_TRACK, 0, MediaKeyPrevious);
            RegisterHotKey(handle, VK_MEDIA_STOP, 0, MediaKeyStop);
            RegisterHotKey(handle, VK_MEDIA_PLAY_PAUSE, 0, MediaKeyPlayPause);

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
                if ((int)msg.wParam == MediaKeyPlayPause)
                {
                    // ...
                }
            }
        }
    }
}
