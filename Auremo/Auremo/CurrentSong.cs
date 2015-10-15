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

using Auremo.MusicLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Auremo
{
    public class CurrentSong : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        #endregion

        private DataModel m_DataModel = null;
        private Playable m_Playable = null;

        public CurrentSong(DataModel dataModel)
        {
            m_DataModel = dataModel;
            m_DataModel.ServerStatus.PropertyChanged += new PropertyChangedEventHandler(OnServerStatusPropertyChanged);
            Update();
        }

        public void Update()
        {
            m_DataModel.ServerSession.CurrentSong();
        }

        public void OnCurrentSongResponseReceived(IEnumerable<MPDSongResponseBlock> response)
        {
            if (response.Count() > 0)
            {
                Playable = PlayableFactory.CreatePlayable(response.First());
            }
        }

        public Playable Playable
        {
            get
            {
                return m_Playable;
            }
            private set
            {
                if (value != m_Playable)
                {
                    m_Playable = value;
                    NotifyPropertyChanged("Playable");
                    NotifyPropertyChanged("DisplayString");
                }
            }
        }
        
        public string DisplayString
        {
            get
            {
                StringBuilder result = new StringBuilder();

                if (m_DataModel.ServerStatus.IsPlaying)
                {
                    result.Append("Playing");
                }
                else if (m_DataModel.ServerStatus.IsPaused)
                {
                    result.Append("Paused");
                }
                else if (m_DataModel.ServerStatus.IsStopped)
                {
                    result.Append("Stopped");
                }

                if (Playable != null)
                {
                    result.Append(" - ");

                    if (Playable is AudioStream)
                    {
                        AudioStream stream = Playable as AudioStream;

                        if (stream.Title != null)
                        {
                            result.Append(stream.Title);
                            result.Append(" - ");
                        }

                        result.Append(stream.Name ?? stream.Label ?? stream.Path.ToString());
                    }
                    else if (Playable is Link)
                    {
                        Link link = Playable as Link;

                        if (link.Artist != null)
                        {
                            result.Append(link.Artist);
                            result.Append(": ");
                        }

                        result.Append(link.Title ?? link.Path.ToString());

                        if (link.Album != null || link.Date != null)
                        {
                            result.Append(" (");

                            if (link.Album != null)
                            {
                                result.Append(link.Album);

                                if (link.Date != null)
                                {
                                    result.Append(", ");
                                }
                            }

                            if (link.Date != null)
                            {
                                result.Append(link.Date);
                            }

                            result.Append(")");
                        }
                    }
                    else
                    {
                        result.Append(Playable.Title);
                    }
                }

                result.Append(".");

                if (m_DataModel.ServerStatus.AudioQuality.Length > 0)
                {
                    result.Append(" ");
                    result.Append(m_DataModel.ServerStatus.AudioQuality);
                    result.Append(".");
                }

                return result.ToString();
            }
        }

        private void OnServerStatusPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CurrentSongIndex" || e.PropertyName == "PlaylistVersion")
            {
                Update();
            }
            else if (e.PropertyName == "State")
            {
                NotifyPropertyChanged("DisplayString");
            }
        }
    }
}
