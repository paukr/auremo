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
using System.Linq;
using System.Text;
using System.Threading;

namespace Auremo
{
    public class QuickSearchThread
    {
        QuickSearch m_Owner = null;
        Database m_Database = null;
        object m_Lock = new object();
        string[] m_Fragments = new string[0];
        bool m_SearchChanged = false;
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
                throw e;
            }
        }

        private void Run()
        {
            IEnumerable<SongMetadata> allSongs = m_Database.Songs;

            string[] fragments = new string[0];
            bool searchChanged = false;
            bool terminating = false;

            while (!terminating)
            {
                lock (m_Lock)
                {
                    fragments = m_Fragments;
                    searchChanged = m_SearchChanged;
                    terminating = m_Terminating;
                    m_SearchChanged = false;
                    m_Event.Reset();
                }

                if (!terminating)
                {
                    for (int i = 0; i < fragments.Count(); ++i)
                    {
                        fragments[i] = fragments[i].ToLower();
                    }

                    IList<SongMetadata> newResults = new List<SongMetadata>();

                    if (searchChanged && fragments.Count() > 0)
                    {
                        DateTime lastUpdate = DateTime.MinValue;

                        foreach (SongMetadata song in allSongs)
                        {
                            bool allFragmentsMatch = true;

                            for (int i = 0; i < fragments.Count() && allFragmentsMatch; ++i)
                            {
                                string fragment = fragments[i];
                                allFragmentsMatch = Match(song.Artist, fragment) || Match(song.Album, fragment) || Match(song.Title, fragment);
                            }

                            if (allFragmentsMatch)
                            {
                                newResults.Add(song);

                                if (DateTime.Now.Subtract(lastUpdate).TotalMilliseconds >= 500)
                                {
                                    lock (m_Lock)
                                    {
                                        searchChanged = m_SearchChanged;
                                        terminating = m_Terminating;
                                        m_Event.Reset();
                                    }

                                    if (!terminating && !searchChanged)
                                    {
                                        m_Owner.AddSearchResults(newResults);
                                        newResults = new List<SongMetadata>();
                                        lastUpdate = DateTime.Now;
                                    }
                                }
                            }
                        }
                    }

                    if (!terminating && !searchChanged)
                    {
                        if (newResults.Count > 0)
                        {
                            m_Owner.AddSearchResults(newResults);
                        }

                        m_Event.WaitOne();
                    }
                }
            }
        }

        public void OnSearchStringChanged(string[] fragments)
        {
            lock (m_Lock)
            {
                m_Fragments = fragments;
                m_SearchChanged = true;
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

        private static bool Match(string tag, string fragment)
        {
            if (tag == null)
            {
                return false;
            }

            return tag.ToLower().Contains(fragment);
        }
    }
}
