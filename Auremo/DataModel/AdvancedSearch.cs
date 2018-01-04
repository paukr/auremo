﻿/*
 * Copyright 2016 Mikko Teräs and Niilo Säämänen.
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Auremo
{
    public class AdvancedSearch
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

        private DataModel m_DataModel = null;
        private DateNormalizer m_DateNormalizer = null;
        private bool m_IncludeLocal = true;
        private bool m_IncludeSpotify = true;
        private SearchType m_SearchType = SearchType.Any;
        string m_SearchString = "";
        IList<LibraryItem> m_UnfilteredSearchResults = new List<LibraryItem>();

        public AdvancedSearch(DataModel dataModel)
        {
            m_DataModel = dataModel;
            string[] dateFormat = { "YYYY" };
            m_DateNormalizer = new DateNormalizer(dateFormat);
            SearchResults = new ObservableCollection<IndexedLibraryItem>();

            m_DataModel.ServerSession.PropertyChanged += new PropertyChangedEventHandler(OnServerSessionPropertyChanged);
        }

        public string SearchString
        {
            get
            {
                return m_SearchString;
            }
            set
            {
                if (value != m_SearchString)
                {
                    m_SearchString = value;
                    NotifyPropertyChanged("SearchString");
                }
            }
        }

        public void Search()
        {
            string search = SearchString.Trim();

            if (search.Length > 0)
            {
                m_UnfilteredSearchResults.Clear();
                SearchResults.Clear();
                string type = m_SearchType.ToString().ToLowerInvariant();
                m_DataModel.ServerSession.Send(MPDCommandFactory.Search(type, SearchString));
            }
        }

        public ObservableCollection<IndexedLibraryItem> SearchResults
        {
            get;
            private set;
        }

        public bool IncludeLocal
        {
            get
            {
                return m_IncludeLocal;
            }
            set
            {
                if (m_IncludeLocal != value)
                {
                    m_IncludeLocal = value;
                    NotifyPropertyChanged("IncludeLocal");
                    FilterSearchResults();
                }
            }
        }

        public bool IncludeSpotify
        {
            get
            {
                return m_IncludeSpotify;
            }
            set
            {
                if (m_IncludeSpotify != value)
                {
                    m_IncludeSpotify = value;
                    NotifyPropertyChanged("IncludeSpotify");
                    FilterSearchResults();
                }
            }
        }

        public bool SearchByAny
        {
            get
            {
                return m_SearchType == SearchType.Any;
            }
            set
            {
                if (value && m_SearchType != SearchType.Any)
                {
                    m_SearchType = SearchType.Any;
                    NotifyPropertyChanged("SearchByAny");
                }
            }
        }

        public bool SearchByArtist
        {
            get
            {
                return m_SearchType == SearchType.Artist;
            }
            set
            {
                if (value && m_SearchType != SearchType.Artist)
                {
                    m_SearchType = SearchType.Artist;
                    NotifyPropertyChanged("SearchByArtist");
                }
            }
        }

        public bool SearchByAlbum
        {
            get
            {
                return m_SearchType == SearchType.Album;
            }
            set
            {
                if (value && m_SearchType != SearchType.Album)
                {
                    m_SearchType = SearchType.Album;
                    NotifyPropertyChanged("SearchByAlbum");
                }
            }
        }

        public bool SearchByTitle
        {
            get
            {
                return m_SearchType == SearchType.Title;
            }
            set
            {
                if (value && m_SearchType != SearchType.Title)
                {
                    m_SearchType = SearchType.Title;
                    NotifyPropertyChanged("SearchByTitle");
                }
            }
        }

        public void OnSearchResponseReceived(IEnumerable<MPDSongResponseBlock> response)
        {
            m_UnfilteredSearchResults.Clear();

            foreach (MPDSongResponseBlock item in response)
            {
                m_UnfilteredSearchResults.Add(PlayableFactory.CreatePlayable(item, m_DataModel) as LibraryItem);
            }

            FilterSearchResults();
        }

        private void FilterSearchResults()
        {
            SearchResults.Clear();

            foreach (LibraryItem item in m_UnfilteredSearchResults)
            {
                if (item is Song && IncludeLocal || item is Link && IncludeSpotify)
                {
                    SearchResults.Add(new IndexedLibraryItem(item, SearchResults.Count));
                }
            }
        }

        private void OnServerSessionPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "State")
            {
                if (m_DataModel.ServerSession.State != ServerSession.SessionState.Connected)
                {
                    SearchString = "";
                    m_UnfilteredSearchResults.Clear();
                    SearchResults.Clear();
                }
            }
        }
    }
}
