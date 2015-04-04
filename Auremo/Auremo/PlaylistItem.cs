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
    public class PlaylistItem : DataGridItem, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged implementation

        // At present only the IsPlaying property sends notifications.
        // All other properties should be set to their final state
        // before the playlist item is inserted into the ListView.
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

        public PlaylistItem()
        {
            Playable = null;
            Id = -1;
            Position = -1;
            IsSelected = false;
            IsPlaying = false;
            IsPaused = false;
        }

        public object Content
        {
            get
            {
                return Playable;
            }
        }

        public Playable Playable
        {
            get;
            set;
        }

        public int Id
        {
            get;
            set;
        }

        public int Position
        {
            get;
            set;
        }

        public bool IsSelected
        {
            get;
            set;
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

        public bool IsValid
        {
            get
            {
                return Playable != null && Id >= 0;
            }
        }

        public override string ToString()
        {
            return Id + " - " + Playable.ToString();
        }
    }
}
