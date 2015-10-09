using System;
using System.Windows.Media;

namespace Auremo
{
    public class CoverFetchedEventArgs : EventArgs
    {
        public CoverFetchedEventArgs(string artist, string album, ImageSource cover)
        {
            Artist = artist;
            Album = album;
            Cover = cover;
        }

        public string Artist
        {
            get;
            private set;
        }

        public string Album
        {
            get;
            private set;
        }

        public ImageSource Cover
        {
            get;
            private set;
        }
    }
}
