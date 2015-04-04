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

using Auremo.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Auremo
{
    public class Database : INotifyPropertyChanged
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
        private IDictionary<string, ISet<AlbumMetadata>> m_AlbumsByArtist = new SortedDictionary<string, ISet<AlbumMetadata>>();
        private IDictionary<string, IDictionary<string, AlbumMetadata>> m_AlbumsByArtistAndName = new SortedDictionary<string, IDictionary<string, AlbumMetadata>>();
        private IDictionary<string, ISet<AlbumMetadata>> m_AlbumsByGenre = new SortedDictionary<string, ISet<AlbumMetadata>>();
        private IDictionary<AlbumMetadata, ISet<string>> m_SongPathsByAlbum = new SortedDictionary<AlbumMetadata, ISet<string>>();
        private IDictionary<SongMetadata, AlbumMetadata> m_AlbumBySong = new SortedDictionary<SongMetadata, AlbumMetadata>();
        private IDictionary<string, SongMetadata> m_LocalSongCollection = new SortedDictionary<string, SongMetadata>(StringComparer.Ordinal);
        private IDictionary<string, SongMetadata> m_SpotifySongCollection = new SortedDictionary<string, SongMetadata>(StringComparer.Ordinal);

        public Database(DataModel dataModel)
        {
            m_DataModel = dataModel;
            Artists = new List<string>();
            Genres = new List<string>();
            AlbumSortRule = new AlbumByDateComparer();

            m_DataModel.ServerSession.PropertyChanged += new PropertyChangedEventHandler(OnServerSessionPropertyChanged);
            m_DataModel.ServerStatus.PropertyChanged += new PropertyChangedEventHandler(OnServerStatusPropertyChanged);
        }

        public void ClearCollection()
        {
            m_AlbumsByArtist.Clear();
            m_AlbumsByGenre.Clear();
            m_SongPathsByAlbum.Clear();
            m_AlbumBySong.Clear();
            m_LocalSongCollection.Clear();
            m_SpotifySongCollection.Clear();

            Artists = new List<string>();
            Genres = new List<string>();

            NotifyPropertyChanged("Database");
        }

        public void RefreshCollection()
        {
            ProcessSettings();
            m_SongPathsByAlbum = new SortedDictionary<AlbumMetadata, ISet<string>>(AlbumSortRule);
            QuerySongInfo();
        }

        public void OnListAllInfoResponseReceived(IEnumerable<MPDSongResponseBlock> response)
        {
            ClearCollection();

            PopulateSongInfo(response);
            PopulateArtists();
            PopulateGenres();
            PopulateAlbumsByArtist();
            PopulateSongPathsByAlbum();
            PopulateAlbumsBySong();
            PopulateAlbumsByGenre();

            NotifyPropertyChanged("Database");
        }

        public IEnumerable<string> Artists
        {
            get;
            private set;
        }

        public IEnumerable<string> Genres
        {
            get;
            private set;
        }

        public IEnumerable<SongMetadata> Songs
        {
            get
            {
                return m_LocalSongCollection.Values;
            }
        }

        public ISet<AlbumMetadata> AlbumsByArtist(string artist)
        {
            ISet<AlbumMetadata> result = new SortedSet<AlbumMetadata>(AlbumSortRule);

            if (m_AlbumsByArtist.ContainsKey(artist))
            {
                foreach (AlbumMetadata album in m_AlbumsByArtist[artist])
                {
                    result.Add(album);
                }
            }

            return result;
        }

        public ISet<AlbumMetadata> AlbumsByGenre(string genre)
        {
            ISet<AlbumMetadata> result = new SortedSet<AlbumMetadata>(AlbumSortRule);

            if (m_AlbumsByGenre.ContainsKey(genre))
            {
                foreach (AlbumMetadata album in m_AlbumsByGenre[genre])
                {
                    result.Add(album);
                }
            }

            return result;
        }

        public AlbumMetadata AlbumOfSong(SongMetadata song)
        {
            AlbumMetadata album = null;

            if (m_AlbumBySong.TryGetValue(song, out album))
            {
                return album;
            }
            else
            {
                return null;
            }
        }

        public ISet<SongMetadata> SongsByAlbum(AlbumMetadata byAlbum)
        {
            SortedSet<SongMetadata> result = new SortedSet<SongMetadata>();

            if (m_SongPathsByAlbum.ContainsKey(byAlbum))
            {
                foreach (string path in m_SongPathsByAlbum[byAlbum])
                {
                    if (m_LocalSongCollection.ContainsKey(path))
                    {
                        result.Add(m_LocalSongCollection[path]);
                    }
                }
            }

            return result;
        }

        public SongMetadata SongByPath(string path)
        {
            SongMetadata result;
            
            if (m_LocalSongCollection.TryGetValue(path, out result))
            {
                return result;
            }
            else if (m_SpotifySongCollection.TryGetValue(path, out result))
            {
                return result;
            }

            return null;
        }

        public IComparer<AlbumMetadata> AlbumSortRule
        {
            get;
            private set;
        }

        private void ProcessSettings()
        {
            if (Settings.Default.AlbumSortingMode == AlbumSortingMode.ByDate.ToString())
            {
                AlbumSortRule = new AlbumByDateComparer();
            }
            else
            {
                AlbumSortRule = new AlbumByTitleComparer();
            }
        }

        private void QuerySongInfo()
        {
            m_DataModel.ServerSession.ListAllInfo();
        }
            
        private void PopulateSongInfo(IEnumerable<MPDSongResponseBlock> response)
        {
            foreach (MPDSongResponseBlock item in response)
            {
                Playable playable = item.ToPlayable(m_DataModel);

                if (playable != null && playable is SongMetadata)
                {
                    SongMetadata song = playable as SongMetadata;

                    if (song.IsSpotify)
                    {
                        if (!m_SpotifySongCollection.ContainsKey(song.Path))
                        {
                            m_SpotifySongCollection.Add(song.Path, song);
                        }
                    }
                    else if (song.IsLocal)
                    {
                        if (!m_LocalSongCollection.ContainsKey(song.Path))
                        {
                            m_LocalSongCollection.Add(song.Path, song);
                        }
                    }
                }
            }
        }

        private void PopulateArtists()
        {
            ISet<string> uniqueArtists = new SortedSet<string>();

            foreach (SongMetadata song in m_LocalSongCollection.Values)
            {
                uniqueArtists.Add(song.Artist);
            }

            Artists = uniqueArtists;
        }

        private void PopulateGenres()
        {
            ISet<string> uniqueGenres = new SortedSet<string>();

            foreach (SongMetadata song in m_LocalSongCollection.Values)
            {
                uniqueGenres.Add(song.Genre);
            }

            Genres = uniqueGenres;
        }
        
        private void PopulateAlbumsByArtist()
        {
            // Create a lookup table of unique artist/album pairs. Find
            // the last timestamp for each.
            IDictionary<string, IDictionary<string, string>> artistTitleAndDate = new SortedDictionary<string, IDictionary<string, string>>();

            foreach (SongMetadata song in m_LocalSongCollection.Values)
            {
                if (artistTitleAndDate.ContainsKey(song.Artist))
                {
                    IDictionary<string, string> titleAndDate = artistTitleAndDate[song.Artist];
                    string existingDate = null;
                    titleAndDate.TryGetValue(song.Album, out existingDate);

                    if (existingDate == null || song.Date != null && existingDate.CompareTo(song.Date) < 0)
                    {
                        titleAndDate[song.Album] = song.Date;
                    }
                }
                else
                {
                    IDictionary<string, string> titleAndDate = new SortedDictionary<string, string>();
                    titleAndDate[song.Album] = song.Date;
                    artistTitleAndDate[song.Artist] = titleAndDate;
                }
            }

            // Now create the proper AlbumMetadata objects and add them to
            // the various lookups.
            foreach (string artist in artistTitleAndDate.Keys)
            {
                IDictionary<string, string> titleAndDate = artistTitleAndDate[artist];
                m_AlbumsByArtist[artist] = new SortedSet<AlbumMetadata>(AlbumSortRule);
                ISet<AlbumMetadata> albumsByArtist = m_AlbumsByArtist[artist];
                m_AlbumsByArtistAndName[artist] = new SortedDictionary<string, AlbumMetadata>();
                IDictionary<string, AlbumMetadata> albumsByArtistAndName = m_AlbumsByArtistAndName[artist];

                foreach (string album in artistTitleAndDate[artist].Keys)
                {
                    AlbumMetadata node = new AlbumMetadata(artist, album, titleAndDate[album]);
                    albumsByArtist.Add(node);
                    albumsByArtistAndName[album] = node;
                }
            }
        }

        private void PopulateSongPathsByAlbum()
        {
            foreach (SongMetadata song in m_LocalSongCollection.Values)
            {
                AlbumMetadata album = m_AlbumsByArtistAndName[song.Artist][song.Album];

                if (!m_SongPathsByAlbum.ContainsKey(album))
                {
                    m_SongPathsByAlbum[album] = new SortedSet<string>();
                }

                m_SongPathsByAlbum[album].Add(song.Path);
            }
        }

        private void PopulateAlbumsBySong()
        {
            foreach (KeyValuePair<AlbumMetadata, ISet<string>> albumAndSongs in m_SongPathsByAlbum)
            {
                foreach (string songPath in albumAndSongs.Value)
                {
                    m_AlbumBySong.Add(SongByPath(songPath), albumAndSongs.Key);
                }
            }
        }

        private void PopulateAlbumsByGenre()
        {
            foreach (SongMetadata song in m_LocalSongCollection.Values)
            {
                if (!m_AlbumsByGenre.ContainsKey(song.Genre))
                {
                    m_AlbumsByGenre[song.Genre] = new SortedSet<AlbumMetadata>(AlbumSortRule);
                }

                m_AlbumsByGenre[song.Genre].Add(m_AlbumBySong[song]);
            }
        }

        private void OnServerSessionPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "State")
            {
                if (m_DataModel.ServerSession.State == ServerSession.SessionState.Connected)
                {
                    RefreshCollection();
                }
                else
                {
                    ClearCollection();
                }
            }
        }

        private void OnServerStatusPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "DatabaseUpdateTime")
            {
                if (m_DataModel.ServerSession.State == ServerSession.SessionState.Connected)
                {
                    RefreshCollection();
                }
                else
                {
                    ClearCollection();
                }
            }
        }
    }
}
