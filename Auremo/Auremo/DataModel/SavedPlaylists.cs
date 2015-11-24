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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

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
        private IDictionary<string, SavedPlaylist> m_Playlists = new SortedDictionary<string, SavedPlaylist>(StringComparer.Ordinal);
        private IDictionary<SavedPlaylist, IList<LibraryItem>> m_PlaylistContents = new SortedDictionary<SavedPlaylist, IList<LibraryItem>>();
        private string m_CurrentPlaylistName = "";

        public SavedPlaylists(DataModel dataModel)
        {
            m_DataModel = dataModel;
            Items = new ObservableCollection<IndexedLibraryItem>();
            ItemsOnSelectedPlaylist = new ObservableCollection<IndexedLibraryItem>();
        
            m_DataModel.ServerSession.PropertyChanged += new PropertyChangedEventHandler(OnServerSessionPropertyChanged);
        }

        public void Clear()
        {
            m_Playlists.Clear();
            m_PlaylistContents.Clear();
            Items.Clear();
        }

        public void Refresh()
        {
            m_DataModel.ServerSession.Send(MPDCommandFactory.LsInfo());
        }

        public void OnLsInfoResponseReceived(IEnumerable<MPDResponseLine> response)
        {
            Clear();
            ISet<SavedPlaylist> playlists = new SortedSet<SavedPlaylist>();

            foreach (MPDResponseLine line in response)
            {
                if (line.Key == MPDResponseLine.Keyword.Playlist)
                {
                    SavedPlaylist playlist = new SavedPlaylist(line.Value);
                    playlists.Add(playlist);
                    m_Playlists[line.Value] = playlist;
                    m_PlaylistContents[playlist] = new List<LibraryItem>();
                    m_DataModel.ServerSession.Send(MPDCommandFactory.ListPlaylistInfo(playlist.Title));
                }
            }

            Items.CreateFrom(playlists);
        }

        public void OnListPlaylistInfoResponseReceived(string name, IEnumerable<MPDSongResponseBlock> response)
        {
            IList<LibraryItem> contents = new List<LibraryItem>();

            foreach (MPDSongResponseBlock block in response)
            {
                Playable playable = PlayableFactory.CreatePlayable(block, m_DataModel);

                // If this stream is a part of the collection, use the database version
                // instead of the constructed one so we can display the user-set label.
                if (playable is AudioStream)
                {
                    AudioStream stream = m_DataModel.StreamsCollection.StreamByPath(playable.Path);

                    if (stream != null)
                    {
                        playable = stream;
                    }
                }

                contents.Add((LibraryItem)playable);
            }

            SavedPlaylist playlist = m_Playlists[name];
            m_PlaylistContents[playlist] = contents;
            IEnumerable<LibraryItem> selection = Items.SelectedItems();

            if (selection.Count() == 1 && ((AudioStream)selection.First()).Name == name)
            {
                NotifyPropertyChanged("ItemsOnSelectedPlaylist");
            }
        }

        public ObservableCollection<IndexedLibraryItem> Items
        {
            get;
            private set;
        }

        public void OnSelectedSavedPlaylistChanged()
        {
            ItemsOnSelectedPlaylist.Clear();
            SavedPlaylist selection = SelectedSavedPlaylist;

            if (selection != null && m_PlaylistContents.ContainsKey(selection))
            {
                ItemsOnSelectedPlaylist.CreateFrom(m_PlaylistContents[selection]);
            }
        }

        public SavedPlaylist SelectedSavedPlaylist
        {
            get
            {
                IEnumerable<LibraryItem> selection = Items.SelectedItems();
                
                if (selection.Count() == 1)
                {
                    return (SavedPlaylist)selection.First();
                }
                else
                {
                    return null;
                }
            }
        }

        public ObservableCollection<IndexedLibraryItem> ItemsOnSelectedPlaylist
        {
            get;
            private set;
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
