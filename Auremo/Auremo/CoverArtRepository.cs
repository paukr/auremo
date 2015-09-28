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
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Auremo
{
    public class CoverArtRepository : INotifyPropertyChanged
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
        private IDictionary<string, IDictionary<string, ImageSource>> m_Covers = new SortedDictionary<string, IDictionary<string, ImageSource>>();
        private ImageSource m_CoverLoadingImage = null;
        private ImageSource m_CoverUnavailableImage = null;
        private CoverArtFetchThread m_Fetcher = null;
        private Thread m_Thread = null;
        private object m_Lock = new object();
        private ManualResetEvent m_Event = new ManualResetEvent(false);
        private IList<Tuple<string, string>> m_CoverDeletionRequests = new List<Tuple<string, string>>();
        private IList<Album> m_HighPriorityRequests = new List<Album>();
        private IList<Album> m_NormalPriorityRequests = new List<Album>();
        private IList<Album> m_LowPriorityRequests = new List<Album>();
        private Album m_CurrentlyRunningRequest = null;
        private IList<Tuple<Album, ImageSource>> m_CompletedRequests = new List<Tuple<Album, ImageSource>>();
        private bool m_Terminating = false;

        enum Priority
        {
            High, Normal, Low
        };

        public CoverArtRepository(DataModel dataModel)
        {
            m_DataModel = dataModel;

            m_CoverLoadingImage = new BitmapImage(new Uri("pack://application:,,,/Auremo;component/Graphics/Auremo_icon_128.png", UriKind.Absolute));
            m_CoverUnavailableImage = m_CoverLoadingImage;

            m_Fetcher = new CoverArtFetchThread(this);
            m_Thread = new Thread(new ThreadStart(m_Fetcher.Start));
            m_Thread.Name = "Covert art fetch thread";
            m_Thread.Start();
        }
    
        public void SetCoverOfAlbum(Album album)
        {
            FetchCoverIfMissing(album, Priority.Normal);
        }

        /*
        public void SetCoversOfEntireDatabase()
        {
            foreach (Album album in m_DataModel.Database.Albums)
            {
                FetchCoverIfMissing(album, Priority.Low);
            }
        }
        */

        public void RemoveCover(string artist, string album)
        {
            if (m_Covers.ContainsKey(artist))
            {
                IDictionary<string, ImageSource> albums = m_Covers[artist];

                if (albums.ContainsKey(album))
                {
                    albums.Remove(album);
                }
            }

            lock (m_Lock)
            {
                m_CoverDeletionRequests.Add(new Tuple<string, string>(artist, album));
                m_Event.Set();
            }
        }

        public void Terminate()
        {
            if (m_Fetcher != null)
            {
                lock (m_Lock)
                {
                    m_Terminating = true;
                    m_Event.Set();
                }

                m_Thread.Join();
                m_Fetcher = null;
                m_Thread = null;
            }
        }

        public bool GetRequest(out string artist, out string album, out bool deleteRequested)
        {
            artist = null;
            album = null;
            m_CurrentlyRunningRequest = null;
            deleteRequested = false;
            bool done = false;

            while (!done)
            {
                lock (m_Lock)
                {
                    if (m_Terminating)
                    {
                        return false;
                    }
                    else
                    {
                        if (m_CoverDeletionRequests.Count > 0)
                        {
                            m_CurrentlyRunningRequest = null;
                            artist = m_CoverDeletionRequests[0].Item1;
                            album = m_CoverDeletionRequests[0].Item2;
                            m_CoverDeletionRequests.RemoveAt(0);
                            done = true;
                        }
                        if (m_HighPriorityRequests.Count > 0)
                        {
                            m_CurrentlyRunningRequest = m_HighPriorityRequests[0];
                            artist = m_HighPriorityRequests[0].Artist.Name;
                            album = m_HighPriorityRequests[0].Title;
                            m_HighPriorityRequests.RemoveAt(0);
                            done = true;
                        }
                        else if (m_NormalPriorityRequests.Count > 0)
                        {
                            m_CurrentlyRunningRequest = m_NormalPriorityRequests[0];
                            artist = m_NormalPriorityRequests[0].Artist.Name;
                            album = m_NormalPriorityRequests[0].Title;
                            m_NormalPriorityRequests.RemoveAt(0);
                            done = true;
                        }
                        else if (m_LowPriorityRequests.Count > 0)
                        {
                            m_CurrentlyRunningRequest = m_LowPriorityRequests[0];
                            artist = m_LowPriorityRequests[0].Artist.Name;
                            album = m_LowPriorityRequests[0].Title;
                            m_LowPriorityRequests.RemoveAt(0);
                            done = true;
                        }
                    }
                }

                if (!done)
                {
                    m_Event.WaitOne();
                    m_Event.Reset();
                }
            }
                        
            return true;
        }

        public void CoverArtFetchFinished(ImageSource cover)
        {
            lock (m_Lock)
            {
                m_CompletedRequests.Add(new Tuple<Album, ImageSource>(m_CurrentlyRunningRequest, cover));
                m_CurrentlyRunningRequest = null;
            }

            m_DataModel.MainWindow.Dispatcher.BeginInvoke((Action)OnCoverArtFetchFinished, null);
        }

        private void OnCoverArtFetchFinished()
        {
            IList<Tuple<Album, ImageSource>> completedRequests = null;

            lock (m_Lock)
            {
                completedRequests = m_CompletedRequests;
                m_CompletedRequests = new List<Tuple<Album, ImageSource>>();
            }

            foreach (Tuple<Album, ImageSource> request in completedRequests)
            {
                Album album = request.Item1;
                ImageSource cover = request.Item2;

                /*
                album.Cover = cover;

                if (cover == null)
                {
                    m_Covers[album.Artist.Name][album.Title] = m_CoverUnavailableImage;
                }
                else
                {
                    m_Covers[album.Artist.Name][album.Title] = album.Cover;
                }
                */
            }
        }   

        private void FetchCoverIfMissing(Album album, Priority priority)
        {
            if (album == null)
            {
                return;
            }
            else if (album.Cover != null && album.Cover != m_CoverLoadingImage && album.Cover != m_CoverUnavailableImage)
            {
                return;
            }

            if (!CoverIsSearchable(album))
            {
                album.Cover = m_CoverUnavailableImage;
                return;
            }

            EnsureLookupEntryExists(album);

            if (m_Covers[album.Artist.Name][album.Title] == null)
            {
                m_Covers[album.Artist.Name][album.Title] = m_CoverLoadingImage;
                EnqueueRequst(album, priority);
            }
        }

        private void EnsureLookupEntryExists(Album album)
        {
            if (!m_Covers.ContainsKey(album.Artist.Name))
            {
                m_Covers.Add(album.Artist.Name, new SortedDictionary<string, ImageSource>());
            }

            IDictionary<string, ImageSource> albums = m_Covers[album.Artist.Name];

            if (!albums.ContainsKey(album.Title))
            {
                albums[album.Title] = null;
            }
        }

        private void EnqueueRequst(Album album, Priority priority)
        {
            if (album.Cover == null)
            {
                lock (m_Lock)
                {
                    if (priority == Priority.High)
                    {
                        m_HighPriorityRequests.Add(album);
                    }
                    else if (priority == Priority.Normal)
                    {
                        m_NormalPriorityRequests.Add(album);
                    }
                    else if (priority == Priority.Low)
                    {
                        m_LowPriorityRequests.Add(album);
                    }

                    m_Event.Set();
                }
            }
        }

        private bool CoverIsSearchable(Album album)
        {
            return album != null &&
                album.Artist != null && album.Artist.Name != "" && album.Artist.Name != Artist.Unknown &&
                album.Title != null && album.Title != "" && album.Title != Album.Unknown;
        }
    }
}
