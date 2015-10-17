/*
 * Copyright 2015 Mikko Teräs and Niilo Säämänen.
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
using System.ComponentModel;

namespace Auremo.MusicLibrary
{
    public class AudioStream : LibraryItem, Playable, INotifyPropertyChanged
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

        private string m_Label = null;
        private string m_Title = null;
        private string m_Name = null;

        public AudioStream(Path path, string label)
        {
            Path = path;
            Label = label;
            Title = null;
        }

        public Path Path
        {
            get;
            set;
        }

        /// <summary>
        /// The stream description (e.g., name of the radio station) specified
        /// by the user and/or saved in a M3U/PLS playlist file.
        /// </summary>
        public string Label
        {
            get
            {
                return m_Label;
            }
            set
            {
                if (value != m_Label)
                {
                    m_Label = value;
                    NotifyPropertyChanged("Label");
                    NotifyPropertyChanged("DisplayString");
                }
            }
        }

        /// <summary>
        /// The stream description (e.g., name of the radio station) as given
        /// in a MPD resonse.
        /// </summary>
        public string Name
        {
            get
            {
                return m_Name;
            }
            set
            {
                if (value != m_Name)
                {
                    m_Name = value;
                    NotifyPropertyChanged("Name");
                    NotifyPropertyChanged("DisplayString");
                }
            }
        }

        /// <summary>
        /// The track description (e.g., name of the song currently playing)
        /// as given in a MPD resonse.
        /// </summary>
        public string Title
        {
            get
            {
                return m_Title;
            }
            set
            {
                if (value != m_Title)
                {
                    m_Title = value;
                    NotifyPropertyChanged("Title");
                }
            }
        }

        public override string DisplayString
        {
            get
            {
                return Label ?? Name ?? Path.Full;
            }
        }

        public override int CompareTo(object o)
        {
            if (o is AudioStream)
            {
                return StringComparer.Ordinal.Compare(Path, (o as AudioStream).Path);
            }
            else
            {
                throw new Exception("Stream: attempt to compare to an incompatible object");
            }
        }

        public override string ToString()
        {
            return DisplayString;
        }
    }
}
