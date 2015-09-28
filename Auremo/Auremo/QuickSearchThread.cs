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
using System.Linq;
using System.Text;
using System.Threading;

namespace Auremo
{
    public class QuickSearchThread
    {
        const int ResultUpdateInterval = 250; // Milliseconds
        const int ResultMaxBlockSize = 25;
        QuickSearch m_Owner = null;
        Database m_Database = null;
        object m_Lock = new object();
        int m_SearchId = -1;
        string[] m_Fragments = new string[0];
        bool m_Terminating = false;
        private ManualResetEvent m_Event = new ManualResetEvent(false);

        public QuickSearchThread(QuickSearch owner, Database database)
        {
            m_Owner = owner;
            m_Database = database;
        }

        public void Start()
        {
            try
            {
                Run();
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show("Uncaught exception in Auremo.QuickSearchThread.\n" +
                                               "Please take a screenshot of this message and send it to the developer.\n\n" +
                                               e.ToString(),
                                               "Auremo has crashed!");
                throw;
            }
        }

        private void Run()
        {
            int searchId = -1;
            string[] fragments = new string[0];
            bool terminating = false;

            while (!terminating)
            {
                lock (m_Lock)
                {
                    searchId = m_SearchId;
                    fragments = m_Fragments;
                    terminating = m_Terminating;
                    m_Event.Reset();
                }

                if (!terminating)
                {
                    for (int i = 0; i < fragments.Count(); ++i)
                    {
                        fragments[i] = fragments[i].ToLower();
                    }

                    IList<Song> newResults = new List<Song>();
                    bool searchChanged = false;

                    if (fragments.Count() > 0)
                    {
                        DateTime lastUpdate = DateTime.MinValue;

                        foreach (Song song in m_Database.Songs.Values)
                        {
                            if (Match(song, fragments))
                            {
                                newResults.Add(song);

                                if (newResults.Count > ResultMaxBlockSize || DateTime.Now.Subtract(lastUpdate).TotalMilliseconds >= ResultUpdateInterval)
                                {
                                    lock (m_Lock)
                                    {
                                        searchChanged = searchId != m_SearchId;
                                        searchId = m_SearchId;
                                        terminating = m_Terminating;
                                        m_Event.Reset();
                                    }

                                    if (!terminating && !searchChanged)
                                    {
                                        m_Owner.PostSearchResults(newResults, searchId);
                                        newResults = new List<Song>();
                                        lastUpdate = DateTime.Now;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (!searchChanged && !terminating)
                    {
                        if (newResults.Count > 0)
                        {
                            m_Owner.PostSearchResults(newResults, searchId);
                        }

                        m_Event.WaitOne();
                    }
                }
            }
        }

        public void OnSearchStringChanged(string[] fragments, int searchId)
        {
            lock (m_Lock)
            {
                m_SearchId = searchId;
                m_Fragments = fragments;
                m_Event.Set();
            }
        }

        public void Terminate()
        {
            lock (m_Lock)
            {
                m_Terminating = true;
                m_Event.Set();
            }
        }

        private static bool Match(Song song, string[] fragments)
        {
            string title = song.Title == null ? null : song.Title.ToLower();
            string album = song.Album == null ? null : song.Album.Title.ToLower();
            string artist = song.Artist == null ? null : song.Artist.Name.ToLower();

            return fragments.All(e => artist != null && artist.Contains(e) || album != null && album.Contains(e) || title != null && title.Contains(e));
        }
    }
}
