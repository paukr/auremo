/*
 * Copyright 2014 Mikko Teräs and Niilo Säämänen.
 *
 * This file is part of Auremo.
 *
 * Auremo is free software: you can redistribute it and/or modify it under the
 * terms of the GNU General Public License as published by the Free Software
 * Foundation, version 2.
 *
 * Auremo is distributed in the hope that it will be useful, but WITHOUT ANY
 * WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR
 * A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with Auremo. If not, see http://www.gnu.org/licenses/.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Auremo.MusicLibrary
{
    /// <summary>
    /// A playable that is currently on the playlist.
    /// </summary>
    public class PlaylistItem : LibraryItem, Playable, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        #endregion

        private bool m_IsPlaying = false;
        private bool m_IsPaused = false;

        public PlaylistItem(int id, int position, Path path, string title, string artist, string album)
        {
            Id = id;
            Position = position;
            Path = path;
            Title = title;
            Artist = artist;
            Album = album;
        }

        public PlaylistItem(int id, Song song)
        {
            Id = id;
            Path = song.Path;
            Title = song.Title;
            Artist = song.Artist.DisplayString;
            Album = song.Album.DisplayString;
        }

        public int Id
        {
            get;
            private set;
        }

        /// <summary>
        /// This is the same as IndexedLibraryItem. Unfortunately it's needed for moving with drag-drop.
        /// </summary>
        public int Position
        {
            get;
            private set;
        }

        public Path Path
        {
            get;
            private set;
        }

        public string Title
        {
            get;
            private set;
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

        public bool IsPlaying
        {
            get
            {
                return m_IsPlaying;
            }
            set
            {
                if (m_IsPlaying != value)
                {
                    m_IsPlaying = value;
                    NotifyPropertyChanged("IsPlaying");
                }
            }
        }

        public bool IsPaused
        {
            get
            {
                return m_IsPaused;
            }
            set
            {
                if (m_IsPaused != value)
                {
                    m_IsPaused = value;
                    NotifyPropertyChanged("IsPaused");
                }
            }
        }

        public override string DisplayString
        {
            get
            {
                return Title;
            }
        }

        public override int CompareTo(object o)
        {
            if (o is PlaylistItem)
            {
                PlaylistItem rhs = (PlaylistItem)o;
                return Id - rhs.Id;
            }
            else
            {
                throw new Exception("PlaylistItem: attempt to compare to an incompatible object");
            }
        }

        public override string ToString()
        {
            return DisplayString;
        }
    }
}
