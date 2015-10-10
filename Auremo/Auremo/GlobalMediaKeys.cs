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
        
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_HOTKEY = 0x0312;
        private const int VK_MEDIA_NEXT_TRACK = 0xB0;
        private const int VK_MEDIA_PREV_TRACK = 0xB1;
        private const int VK_MEDIA_STOP = 0xB2;
        private const int VK_MEDIA_PLAY_PAUSE = 0xB3;

        private void RegisterGlobalMediaKeys()
        {
            /*
            IntPtr handle = new WindowInteropHelper(this).Handle;
            
            bool b1 = RegisterHotKey(handle, VK_MEDIA_NEXT_TRACK, 0, VK_MEDIA_NEXT_TRACK);
            bool b2 = RegisterHotKey(handle, VK_MEDIA_PREV_TRACK, 0, VK_MEDIA_PREV_TRACK);
            bool b3 = RegisterHotKey(handle, VK_MEDIA_STOP, 0, VK_MEDIA_STOP);
            bool b4 = RegisterHotKey(handle, VK_MEDIA_PLAY_PAUSE, 0, VK_MEDIA_PLAY_PAUSE);
            bool b5 = RegisterHotKey(handle, (int)Key.MediaPlayPause, 0, (int)Key.MediaPlayPause);

            ComponentDispatcher.ThreadPreprocessMessage += ProcessKeyPress;
            */
        }

        private void UnregisterGlobalMediaKeys()
        {
            /*
            ComponentDispatcher.ThreadPreprocessMessage -= ProcessKeyPress;
            IntPtr handle = new WindowInteropHelper(this).Handle;

            UnregisterHotKey(handle, VK_MEDIA_NEXT_TRACK);
            UnregisterHotKey(handle, VK_MEDIA_PREV_TRACK);
            UnregisterHotKey(handle, VK_MEDIA_STOP);
            UnregisterHotKey(handle, VK_MEDIA_PLAY_PAUSE);
            */
        }

        void ProcessKeyPress(ref MSG msg, ref bool handled)
        {
            /*
            //if (msg.message == WM_HOTKEY)
            if (msg.message == WM_KEYDOWN)
            {
                int i = 0;
            }
            */
        }
    }
}
