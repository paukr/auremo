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

namespace Auremo
{
    public class StreamMetadata : Playable, IComparable, INotifyPropertyChanged
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

        private string m_Path = null;
        private string m_Label = null;
        private string m_Title = null;
        private string m_Name = null;

        public StreamMetadata(string path, string label)
        {
            Path = path;
            Label = label;
        }

        public string Path
        {
            get
            {
                return m_Path;
            }
            set
            {
                if (value != m_Path)
                {
                    m_Path = value;
                    NotifyPropertyChanged("Path");
                    NotifyPropertyChanged("DisplayName");
                }
            }
        }

        /// The stream descriptor exctracted from the M3U/PLS file or
        /// given by the user otherwise.
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
                    NotifyPropertyChanged("DisplayName");
                }
            }
        }

        /// The stream descriptor of the stream as given in a MPD resonse.
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
                    NotifyPropertyChanged("DisplayName");
                }
            }
        }

        /// The track descriptor of the stream as given in a MPD resonse.
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
  
        public string Artist
        {
            get
            {
                return null;
            }
        }

        public string Album
        {
            get
            {
                return null;
            }
        }

        public int? Year
        {
            get
            {
                return null;
            }
        }

        public string DisplayName
        {
            get
            {
                if (Label != null)
                {
                    return Label;
                }
                else if (Name != null)
                {
                    return Name;
                }
                else
                {
                    return Path;
                }
            }
        }

        public int CompareTo(object o)
        {
            if (o is StreamMetadata)
            {
                StreamMetadata rhs = (StreamMetadata)o;
                return StringComparer.Ordinal.Compare(Title, rhs.Title);
            }
            else if (o is SongMetadata)
            {
                return 1;
            }
            else
            {
                throw new Exception("StreamMetadata: attempt to compare to an incompatible object");
            }
        }
    }
}
