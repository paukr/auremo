using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Auremo.MusicLibrary
{
    public class SavedPlaylist : LibraryItem
    {
        public SavedPlaylist(string title)
        {
            Title = title;
        }

        public string Title
        {
            get;
            private set;
        }

        public override string DisplayString
        {
            get
            {
                return Title;
            }
        }

        public override string ToString()
        {
            return DisplayString;
        }

        public override int CompareTo(object o)
        {
            if (o is SavedPlaylist)
            {
                SavedPlaylist rhs = (SavedPlaylist)o;
                return StringComparer.Ordinal.Compare(Title, rhs.Title);
            }
            else
            {
                throw new Exception("SavedPlaylist: attempt to compare to an incompatible object");
            }
        }
    }
}
