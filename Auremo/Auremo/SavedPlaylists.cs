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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Auremo
{
    public class SavedPlaylists : INotifyPropertyChanged
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
        private IDictionary<string, IList<Playable>> m_Playlists = new SortedDictionary<string, IList<Playable>>();
        private IList<MusicCollectionItem> m_SelectedItemsOnSelectedPlaylist = new List<MusicCollectionItem>();
        private string m_SelectedPlaylist = null;
        private string m_CurrentPlaylistName = "";

        public SavedPlaylists(DataModel dataModel)
        {
            m_DataModel = dataModel;
            Playlists = new ObservableCollection<string>();
            ItemsOnSelectedPlaylist = new ObservableCollection<MusicCollectionItem>();
            SelectedItemsOnSelectedPlaylist = new ObservableCollection<MusicCollectionItem>();

            m_DataModel.ServerSession.PropertyChanged += new PropertyChangedEventHandler(OnServerSessionPropertyChanged);
        }

        public void Clear()
        {
            m_Playlists.Clear();
            Playlists.Clear();
        }

        public void Refresh()
        {
            m_DataModel.ServerSession.LsInfo();
        }

        public void OnLsInfoResponseReceived(IEnumerable<MPDResponseLine> response)
        {
            Clear();
            ISet<string> playlists = new SortedSet<string>();

            foreach (MPDResponseLine line in response)
            {
                if (line.Key == MPDResponseLine.Keyword.Playlist)
                {
                    playlists.Add(line.Value);
                    m_DataModel.ServerSession.ListPlaylistInfo(line.Value);
                }
            }

            foreach (string playlist in playlists)
            {
                Playlists.Add(playlist);
            }
        }

        public void OnListPlaylistInfoResponseReceived(string name, IEnumerable<MPDSongResponseBlock> response)
        {
            IList<Playable> playlist = new List<Playable>();

            foreach (MPDSongResponseBlock block in response)
            {
                Playable playable = block.ToPlayable(m_DataModel);

                // If this is a stream that is in the collection, use the database version
                // instead of the constructed one so we can display the user-set label.
                if (playable is StreamMetadata)
                {
                    StreamMetadata stream = m_DataModel.StreamsCollection.StreamByPath(playable.Path);

                    if (stream != null)
                    {
                        playable = stream;
                    }
                }

                if (playable != null)
                {
                    playlist.Add(playable);
                }
            }

            m_Playlists[name] = playlist;

            if (name == SelectedPlaylist)
            {
                NotifyPropertyChanged("ItemsOnSelectedPlaylist");
            }
        }

        public IList<string> Playlists
        {
            get;
            private set;
        }

        public string SelectedPlaylist
        {
            get
            {
                return m_SelectedPlaylist;
            }
            set
            {
                m_SelectedPlaylist = value;
                ItemsOnSelectedPlaylist.Clear();

                if (value != null && m_Playlists.ContainsKey(value))
                {
                    foreach (Playable playable in m_Playlists[value])
                    {
                        ItemsOnSelectedPlaylist.Add(new MusicCollectionItem(playable, ItemsOnSelectedPlaylist.Count));
                    }
                }
            }
        }

        public ObservableCollection<MusicCollectionItem> ItemsOnSelectedPlaylist
        {
            get;
            private set;
        }

        public IList<MusicCollectionItem> SelectedItemsOnSelectedPlaylist
        {
            get
            {
                return m_SelectedItemsOnSelectedPlaylist;
            }
            set
            {
                m_SelectedItemsOnSelectedPlaylist = value;
                NotifyPropertyChanged("SelectedItemsOnSelectedPlaylist");
            }
        }

        public IEnumerable<Playable> PlaylistContents(string playlistName)
        {
            return m_Playlists[playlistName];
        }

        public string CurrentPlaylistName
        {
            get
            {
                return m_CurrentPlaylistName;
            }
            set
            {
                if (value != m_CurrentPlaylistName)
                {
                    m_CurrentPlaylistName = value;
                    NotifyPropertyChanged("CurrentPlaylistName");
                    NotifyPropertyChanged("CurrentPlaylistNameEmpty");
                    NotifyPropertyChanged("CurrentPlaylistNameNonempty");
                }
            }
        }

        public bool CurrentPlaylistNameEmpty
        {
            get
            {
                return CurrentPlaylistName.Trim().Length == 0;
            }
        }

        public bool CurrentPlaylistNameNonempty
        {
            get
            {
                return !CurrentPlaylistNameEmpty;
            }
        }

        private Playable GetPlayableByPath(string path)
        {
            Playable result = m_DataModel.Database.SongByPath(path);

            if (result != null)
            {
                return result;
            }

            result = m_DataModel.StreamsCollection.StreamByPath(path);

            if (result != null)
            {
                return result;
            }

            return new UnknownPlayable(path);
        }

        private void OnServerSessionPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "State")
            {
                if (m_DataModel.ServerSession.State == ServerSession.SessionState.Connected)
                {
                    Refresh();
                }
                else if (m_DataModel.ServerSession.State == ServerSession.SessionState.Disconnected)
                {
                    Clear();
                }
            }
        }
    }
}
