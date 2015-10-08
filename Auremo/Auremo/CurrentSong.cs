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

using Auremo.MusicLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
        private ImageSource m_Cover = null;
        private ImageSource m_AuremoLogo = null;

        public CurrentSong(DataModel dataModel)
        {
            m_DataModel = dataModel;
            m_AuremoLogo = new BitmapImage(new Uri("pack://application:,,,/Auremo;component/Graphics/Auremo_icon.png", UriKind.Absolute));

            m_DataModel.ServerStatus.PropertyChanged += new PropertyChangedEventHandler(OnServerStatusPropertyChanged);
            m_DataModel.CoverArtRepository.CoverFetched += new CoverArtRepository.CoverArtFetchedHandler(OnCoverFetched);

            Update();
        }

        public void Update()
        {
            m_DataModel.ServerSession.CurrentSong();
        }

        public void OnCurrentSongResponseReceived(IEnumerable<MPDSongResponseBlock> responses)
        {
            if (responses.Count() > 0)
            {
                MPDSongResponseBlock response = responses.First();
                Playable = PlayableFactory.CreatePlayable(response);
                Cover = m_DataModel.CoverArtRepository.FetchCoverOfAlbum(response.Artist, response.Album, CoverArtRepository.Priority.High) ?? m_AuremoLogo;
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

                result.Append(". ");
                result.Append(m_DataModel.ServerStatus.AudioQuality);
                result.Append(".");

                return result.ToString();
            }
        }

        public ImageSource Cover
        {
            get
            {
                return m_Cover;
            }
            set
            {
                if (value != m_Cover)
                {
                    m_Cover = value;
                    NotifyPropertyChanged("Cover");
                }
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

        private void OnCoverFetched(object sender, CoverFetchedEventArgs e)
        {
            // TODO: match artist and album name first.
            Cover = e.Cover ?? m_AuremoLogo;
        }
    }
}
