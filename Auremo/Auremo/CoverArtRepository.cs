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

        public delegate void CoverArtFetchedHandler(string artist, string album, ImageSource cover);
        public event CoverArtFetchedHandler CoverFetched;

        private DataModel m_DataModel = null;
        private IDictionary<string, IDictionary<string, ImageSource>> m_Covers = new SortedDictionary<string, IDictionary<string, ImageSource>>();
        private ImageSource m_CoverLoadingImage = null;
        private ImageSource m_CoverUnavailableImage = null;
        private CoverArtFetchThread m_Fetcher = null;

        private Thread m_Thread = null;
        private object m_Lock = new object();
        private ManualResetEvent m_Event = new ManualResetEvent(false);
        private Queue<CoverArtFetchTask> m_CoverDeletionRequests = new Queue<CoverArtFetchTask>();
        private Queue<CoverArtFetchTask> m_HighPriorityRequests = new Queue<CoverArtFetchTask>();
        private Queue<CoverArtFetchTask> m_NormalPriorityRequests = new Queue<CoverArtFetchTask>();
        private Queue<CoverArtFetchTask> m_LowPriorityRequests = new Queue<CoverArtFetchTask>();

        private IList<Tuple<string, string, ImageSource>> m_CompletedRequests = new List<Tuple<string, string, ImageSource>>();
        private bool m_Terminating = false;

        public enum Priority
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
            m_Thread.Name = "Cover art fetch thread";
            m_Thread.Start();
        }

        public ImageSource FetchCoverOfAlbum(Album album, Priority priority)
        {
            if (album == null || album.Artist == null || album.Title == null)
            {
                return null;
            }
            else
            {
                return FetchCoverOfAlbum(album.Artist.Name, album.Title, priority);
            }
        }

        public ImageSource FetchCoverOfAlbum(string artist, string album, Priority priority)
        {
            if (artist == null || album == null)
            {
                return null;
            }
            else
            {
                EnsureLookupEntryExists(artist);

                if (!m_Covers[artist].ContainsKey(album))
                {
                    m_Covers[artist][album] = m_CoverLoadingImage;

                    lock (m_Lock)
                    {
                        if (priority == Priority.High)
                        {
                            m_HighPriorityRequests.Enqueue(new CoverArtFetchTask(CoverArtFetchTask.RequestType.Fetch, artist, album));
                        }
                        else if (priority == Priority.Normal)
                        {
                            m_NormalPriorityRequests.Enqueue(new CoverArtFetchTask(CoverArtFetchTask.RequestType.Fetch, artist, album));
                        }
                        else
                        {
                            m_LowPriorityRequests.Enqueue(new CoverArtFetchTask(CoverArtFetchTask.RequestType.Fetch, artist, album));
                        }
                            
                        m_Event.Set();
                    }
                }

                return m_Covers[artist][album];
            }
        }

        public void SetCoversOfEntireDatabase()
        {
            foreach (Artist artist in m_DataModel.Database.Artists.Values)
            {
                foreach (Album album in m_DataModel.Database.Expand(artist))
                {
                    FetchCoverOfAlbum(album, Priority.Low);
                }
            }
        }

        public void RemoveCover(Album album)
        {
            if (album != null && album.Artist != null && album.Title != null)
            {
                RemoveCover(album.Artist.Name, album.Title);
            }
        }

        public void RemoveCover(string artist, string album)
        {
            if (artist != null && album != null)
            {
                if (m_Covers.ContainsKey(artist) && m_Covers[artist].ContainsKey(album))
                {
                    m_Covers[artist].Remove(album);

                    if (m_Covers[artist].Count == 0)
                    {
                        m_Covers.Remove(artist);
                    }

                    lock (m_Lock)
                    {
                        m_CoverDeletionRequests.Enqueue(new CoverArtFetchTask(CoverArtFetchTask.RequestType.Delete, artist, album));
                        m_Event.Set();
                    }
                }
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

        /// Returns a new fetch or delete request, or null if the worker is to
        /// terminate. If no requests are pending, block.
        public CoverArtFetchTask PopRequest()
        {
            CoverArtFetchTask result = null;

            while (result == null)
            {
                lock (m_Lock)
                {
                    if (m_Terminating)
                    {
                        return null;
                    }
                    else
                    {
                        if (m_CoverDeletionRequests.Count > 0)
                        {
                            result = m_CoverDeletionRequests.Dequeue();
                        }
                        else if (m_HighPriorityRequests.Count > 0)
                        {
                            result = m_HighPriorityRequests.Dequeue();
                        }
                        else if (m_NormalPriorityRequests.Count > 0)
                        {
                            result = m_NormalPriorityRequests.Dequeue();
                        }
                        else if (m_LowPriorityRequests.Count > 0)
                        {
                            result = m_LowPriorityRequests.Dequeue();
                        }
                    }
                }

                if (result == null)
                {
                    m_Event.WaitOne();
                    m_Event.Reset();
                }
            }
                        
            return result;
        }

        public void CoverArtFetchFinished(string artist, string album, ImageSource cover)
        {
            lock (m_Lock)
            {
                m_CompletedRequests.Add(new Tuple<string, string, ImageSource>(artist, album, cover));
            }

            m_DataModel.MainWindow.Dispatcher.BeginInvoke((Action)OnCoverArtFetchFinished, null);
        }

        private void OnCoverArtFetchFinished()
        {
            IList<Tuple<string, string, ImageSource>> completedRequests = null;

            lock (m_Lock)
            {
                completedRequests = m_CompletedRequests;
                m_CompletedRequests = new List<Tuple<string, string, ImageSource>>();
            }

            foreach (Tuple<string, string, ImageSource> request in completedRequests)
            {
                EnsureLookupEntryExists(request.Item1);
                m_Covers[request.Item1][request.Item2] = request.Item3 ?? m_CoverUnavailableImage;
                CoverFetched(request.Item1, request.Item2, request.Item3);
            }
        }

        private void EnsureLookupEntryExists(string artist)
        {
            if (!m_Covers.ContainsKey(artist))
            {
                m_Covers[artist] = new SortedDictionary<string, ImageSource>();
            }
        }
    }
}
