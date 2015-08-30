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
using System.Threading;

namespace Auremo
{
    public class QuickSearch : INotifyPropertyChanged
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
        private QuickSearchThread m_Searcher = null;
        private Thread m_Thread = null;

        object m_Lock = new object();
        string m_SearchString = "";
        string[] m_SearchStringFragments = new string[0];
        volatile IList<IEnumerable<Song>> m_NewResults = new List<IEnumerable<Song>>();

        public QuickSearch(DataModel dataModel)
        {
            m_DataModel = dataModel;
            SearchResults = new ObservableCollection<IndexedLibraryItem>();

            m_DataModel.ServerSession.PropertyChanged += new PropertyChangedEventHandler(OnServerSessionPropertyChanged);

            m_Searcher = new QuickSearchThread(this, m_DataModel.Database);
            m_Thread = new Thread(new ThreadStart(m_Searcher.Start));
            m_Thread.Name = "QuickSearch thread";
            m_Thread.Start();
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
                    bool searchChanged = UpdateSearchStringFragments(value);

                    if (searchChanged && m_Searcher != null)
                    {
                        lock (m_Lock)
                        {
                            m_Searcher.OnSearchStringChanged(m_SearchStringFragments);
                            m_NewResults.Clear();
                            SearchResults.Clear();
                            
                        }
                    }
                }
            }
        }

        public ObservableCollection<IndexedLibraryItem> SearchResults
        {
            get;
            private set;
        }

        public void Terminate()
        {
            if (m_Searcher != null)
            {
                m_Searcher.Terminate();
                m_Thread.Join();
                m_Searcher = null;
                m_Thread = null;
            }
        }

        public void AddSearchResults(IEnumerable<Song> resultList)
        {
            lock (m_Lock)
            {
                m_NewResults.Add(resultList);
            }

            m_DataModel.MainWindow.Dispatcher.BeginInvoke((Action)OnNewSearchResultsReceived, null);
        }

        private void OnNewSearchResultsReceived()
        {
            IList<IEnumerable<Song>> newResults = null;

            lock (m_Lock)
            {
                newResults = m_NewResults;
                m_NewResults = new List<IEnumerable<Song>>();
            }

            foreach (IEnumerable<Song> resultList in newResults)
            {
                foreach (Song result in resultList)
                {
                    SearchResults.Add(new IndexedLibraryItem(result, SearchResults.Count));
                }
            }
        }

        private bool UpdateSearchStringFragments(string search)
        {
            char[] delimiters = { ' ', '\t' };
            string[] fragments = search.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

            if (!m_SearchStringFragments.SequenceEqual(fragments))
            {
                m_SearchStringFragments = fragments;
                return true;
            }

            return false;
        }

        private void OnServerSessionPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "State")
            {
                if (m_DataModel.ServerSession.State != ServerSession.SessionState.Connected)
                {
                    SearchString = "";
                    SearchResults.Clear();
                }
            }
        }
    }
}
