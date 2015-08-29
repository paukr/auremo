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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Auremo
{
    public class Playlist : INotifyPropertyChanged
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

        DataModel m_DataModel = null;
        IndexedLibraryItem m_ItemMarkedAsCurrent = null;
        int m_NumberOfSelectedLocalSongs = 0;
        int m_NumberOfSelectedSpotifySongs = 0;
        int m_NumberOfSelectedStreams = 0;

        public Playlist(DataModel dataModel)
        {
            m_DataModel = dataModel;
            Items = new ObservableCollection<IndexedLibraryItem>();
            m_DataModel.ServerStatus.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(OnServerStatusPropertyChanged);
        }

        public ObservableCollection<IndexedLibraryItem> Items
        {
            get;
            private set;
        }

        public string PlayStatusDescription
        {
            get; 
            private set;
        }

        public int NumberOfSelectedLocalSongs
        {
            get
            {
                return m_NumberOfSelectedLocalSongs;
            }
            private set
            {
                if (m_NumberOfSelectedLocalSongs != value)
                {
                    m_NumberOfSelectedLocalSongs = value;
                    NotifyPropertyChanged("NumberOfSelectedLocalSongs");
                }
            }
        }

        public int NumberOfSelectedSpotifySongs
        {
            get
            {
                return m_NumberOfSelectedSpotifySongs;
            }
            private set
            {
                if (m_NumberOfSelectedSpotifySongs != value)
                {
                    m_NumberOfSelectedSpotifySongs = value;
                    NotifyPropertyChanged("NumberOfSelectedSpotifySongs");
                }
            }
        }

        public int NumberOfSelectedStreams
        {
            get
            {
                return m_NumberOfSelectedStreams;
            }
            private set
            {
                if (m_NumberOfSelectedStreams != value)
                {
                    m_NumberOfSelectedStreams = value;
                    NotifyPropertyChanged("NumberOfSelectedStreams");
                }
            }
        }

        /*
        public void OnSelectedItemsChanged(IEnumerable<OldPlaylistItem> selection)
        {
            int localSongs = 0;
            int spotifySongs = 0;
            int streams = 0;

            foreach (OldPlaylistItem item in selection)
            {
                SelectedItems.Add(item);

                if (item.Content is SongMetadata)
                {
                    SongMetadata song = item.Content as SongMetadata;

                    if (song.IsLocal)
                    {
                        localSongs += 1;
                    }
                    else if (song.IsSpotify)
                    {
                        spotifySongs += 1;
                    }
                }
                else if (item.Content is StreamMetadata)
                {
                    streams += 1;
                }
            }

            NumberOfSelectedLocalSongs = localSongs;
            NumberOfSelectedSpotifySongs = spotifySongs;
            NumberOfSelectedStreams = streams;
        }
        */ 

        private void OnServerStatusPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "PlaylistVersion")
            {
                if (m_DataModel.ServerStatus.PlaylistVersion >= 0)
                {
                    UpdateItems();
                }
                else
                {
                    Items.Clear();
                }

                UpdateCurrentSong();
            }
            else if (e.PropertyName == "CurrentSongIndex" || e.PropertyName == "State")
            {
                UpdateCurrentSong();
            }
        }

        private void UpdateItems()
        {
            m_DataModel.ServerSession.PlaylistInfo();
        }

        public void OnPlaylistInfoResponseReceived(IEnumerable<MPDSongResponseBlock> response)
        {
            Items.Clear();

            foreach (MPDSongResponseBlock block in response)
            {
                int id = block.Id;
                int position = block.Pos;
                Path path = new Path(block.File);
                string title = block.Title ?? block.File;
                string artist = block.Artist ?? "";
                string album = block.Album ?? "";
                Items.Add(new IndexedLibraryItem(new PlaylistItem(id, position, path, title, artist, album), Items.Count));
            }

            UpdateCurrentSong();
        }

        private void UpdateCurrentSong()
        {
            if (m_ItemMarkedAsCurrent != null)
            {
                m_ItemMarkedAsCurrent.ItemAs<PlaylistItem>().IsPlaying = false;
                m_ItemMarkedAsCurrent.ItemAs<PlaylistItem>().IsPaused = false;
            }

            int current = m_DataModel.ServerStatus.CurrentSongIndex;

            if (current >= 0 && current < Items.Count)
            {
                m_ItemMarkedAsCurrent = Items[current];
                m_ItemMarkedAsCurrent.ItemAs<PlaylistItem>().IsPlaying = m_DataModel.ServerStatus.IsPlaying;
                m_ItemMarkedAsCurrent.ItemAs<PlaylistItem>().IsPaused = m_DataModel.ServerStatus.IsPaused;
            }
        }

        private OldPlayable PlayableByPath(string path)
        {
            /*
            Playable result = m_DataModel.Database.SongByPath(path);

            if (result == null)
            {
                result = m_DataModel.StreamsCollection.StreamByPath(path);
            }

            if (result == null)
            {
                result = new UnknownPlayable(path);
            }

            return result;
             */

            return null;
        }
    }
}
