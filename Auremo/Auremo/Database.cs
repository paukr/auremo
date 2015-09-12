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
using Auremo.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
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

        private IDictionary<Artist, IDictionary<string, Album>> m_AlbumLookup = new SortedDictionary<Artist, IDictionary<string, Album>>();
        private IDictionary<Genre, IDictionary<Album, GenreFilteredAlbum>> m_GenreFilteredAlbumLookup = new SortedDictionary<Genre, IDictionary<Album, GenreFilteredAlbum>>();

        private IDictionary<LibraryItem, ISet<LibraryItem>> m_ArtistExpansion = new SortedDictionary<LibraryItem, ISet<LibraryItem>>();
        private IDictionary<LibraryItem, ISet<LibraryItem>> m_AlbumExpansion = new SortedDictionary<LibraryItem, ISet<LibraryItem>>();
        private IDictionary<LibraryItem, ISet<LibraryItem>> m_GenreExpansion = new SortedDictionary<LibraryItem, ISet<LibraryItem>>();
        private IDictionary<LibraryItem, ISet<LibraryItem>> m_GenreFilteredAlbumExpansion = new SortedDictionary<LibraryItem, ISet<LibraryItem>>();
        private IDictionary<LibraryItem, ISet<LibraryItem>> m_DirectoryExpansion = new SortedDictionary<LibraryItem, ISet<LibraryItem>>();

        private string UnknownArtist = "Unknown Artist";
        private string UnknownGenre = "Unknown Genre";
        private string UnknownAlbum = "Unknown Album";

        public Database(DataModel dataModel)
        {
            m_DataModel = dataModel;
            AlbumSortRule = new AlbumByDateComparer();

            Artists = new SortedDictionary<string, Artist>();
            Genres = new SortedDictionary<string, Genre>();
            Directories = new SortedDictionary<string, Directory>();
            Songs = new SortedDictionary<Path, Song>();

            m_DataModel.ServerSession.PropertyChanged += new PropertyChangedEventHandler(OnServerSessionPropertyChanged);
            m_DataModel.ServerStatus.PropertyChanged += new PropertyChangedEventHandler(OnServerStatusPropertyChanged);
        }

        public void ClearCollection()
        {
            Artists.Clear();
            Genres.Clear();
            Directories.Clear();
            Songs.Clear();

            m_AlbumLookup.Clear();
            m_GenreFilteredAlbumLookup.Clear();

            m_ArtistExpansion.Clear();
            m_AlbumExpansion.Clear();
            m_GenreExpansion.Clear();
            m_GenreFilteredAlbumExpansion.Clear();

            NotifyPropertyChanged("Database");
        }

        public void RefreshCollection()
        {
            ProcessSettings();
            ClearCollection();
            QuerySongInfo();
        }

        public void OnListAllInfoResponseReceived(IEnumerable<MPDSongResponseBlock> response)
        {
            ClearCollection();
            PopulateDatabase(response);
            NotifyPropertyChanged("Database");
        }

        // TODO: this might work as a ConcurrentDictionary.
        public IDictionary<string, Artist> Artists
        {
            get;
            private set;
        }

        // TODO: this might work as a ConcurrentDictionary.
        public IDictionary<string, Genre> Genres
        {
            get;
            private set;
        }

        public IDictionary<string, Directory> Directories
        {
            get;
            private set;
        }

        // TODO: this might work as a ConcurrentDictionary.
        public IDictionary<Path, Song> Songs
        {
            get;
            private set;
        }

        public IComparer<Album> AlbumSortRule
        {
            get;
            private set;
        }

        public IEnumerable<LibraryItem> Expand(LibraryItem parent)
        {
            // TODO: throw exceptions instead of returning safe but useless values.
            if (parent is Playable)
            {
                return new List<LibraryItem>() { parent };
            }
            if (parent is Artist)
            {
                return m_ArtistExpansion.ContainsKey(parent) ? m_ArtistExpansion[parent].ToList<LibraryItem>() : new List<LibraryItem>();
            }
            else if (parent is Album)
            {
                return m_AlbumExpansion.ContainsKey(parent) ? m_AlbumExpansion[parent].ToList<LibraryItem>() : new List<LibraryItem>();
            }
            else if (parent is Genre)
            {
                return m_GenreExpansion.ContainsKey(parent) ? m_GenreExpansion[parent].ToList<LibraryItem>() : new List<LibraryItem>();
            }
            else if (parent is GenreFilteredAlbum)
            {
                return m_GenreFilteredAlbumExpansion.ContainsKey(parent) ? m_GenreFilteredAlbumExpansion[parent].ToList<LibraryItem>() : new List<LibraryItem>();
            }

            throw new Exception("Database.Expand(): unexpected argument type.");
        }

        public IEnumerable<LibraryItem> Expand(IEnumerable<LibraryItem> parents)
        {
            IList<LibraryItem> result = new List<LibraryItem>();

            foreach (LibraryItem parent in parents)
            {
                result.AddAll(Expand(parent));
            }

            return result;
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

        private void PopulateDatabase(IEnumerable<MPDSongResponseBlock> response)
        {
            foreach (MPDSongResponseBlock block in response)
            {
                Song song = new Song(new Path(block.File));
                song.Artist = GetOrCreateArtist(Settings.Default.UseAlbumArtist && block.AlbumArtist != null ? block.AlbumArtist : block.Artist);
                song.Genre = GetOrCreateGenre(block.Genre);
                song.Album = GetOrCreateAlbum(song.Artist, block.Album);
                song.GenreFilteredAlbum = GetOrCreateGenreFilteredAlbum(song.Genre, song.Album);
                song.Directory = GetOrCreateDirectory(song.Path);

                song.Title = block.Title;
                song.Length = block.Time;
                song.Track = block.Track;
                song.Date = m_DataModel.YearNormalizer.Normalize(block.Date);

                Songs[song.Path] = song;
                AddExpansion(song.Album, song);
                AddExpansion(song.GenreFilteredAlbum, song);
            }
        }
        
        private Artist GetOrCreateArtist(string artist)
        {
            string key = artist ?? UnknownArtist;

            if (!Artists.ContainsKey(key))
            {
                Artists[key] = new Artist(key);
            }

            return Artists[key];
        }

        private Genre GetOrCreateGenre(string genre)
        {
            string key = genre ?? UnknownGenre;

            if (!Genres.ContainsKey(key))
            {
                Genres[key] = new Genre(key);
            }

            return Genres[key];
        }

        private Album GetOrCreateAlbum(Artist artist, string title)
        {
            string albumKey = title ?? UnknownAlbum;

            if (!m_AlbumLookup.ContainsKey(artist))
            {
                m_AlbumLookup[artist] = new SortedDictionary<string, Album>(StringComparer.Ordinal);
            }

            IDictionary<string, Album> albumList = m_AlbumLookup[artist];

            if (!albumList.ContainsKey(albumKey))
            {
                Album album = new Album(artist, albumKey, null);
                albumList[albumKey] = album;
                AddExpansion(artist, album);
            }

            return albumList[albumKey];
        }

        private GenreFilteredAlbum GetOrCreateGenreFilteredAlbum(Genre genre, Album album)
        {
            if (!m_GenreFilteredAlbumLookup.ContainsKey(genre))
            {
                m_GenreFilteredAlbumLookup[genre] = new SortedDictionary<Album, GenreFilteredAlbum>();
            }

            IDictionary<Album, GenreFilteredAlbum> albumList = m_GenreFilteredAlbumLookup[genre];

            if (!albumList.ContainsKey(album))
            {
                GenreFilteredAlbum genreFilteredAlbum = new GenreFilteredAlbum(genre, album.Artist, album.Title, album.Date);
                m_GenreFilteredAlbumLookup[genre][album] = genreFilteredAlbum;
                AddExpansion(genre, genreFilteredAlbum);
            }

            return m_GenreFilteredAlbumLookup[genre][album];
        }

        private Directory GetOrCreateDirectory(Path path)
        {
            string[] parts = path.Directories;
            string fullpath = "";
            Directory parentOrResult = null;

            for (int i = 0; i < parts.Count() - 1; ++i)
            {
                fullpath = fullpath + parts[i] + "/";

                if (Directories.ContainsKey(fullpath))
                {
                    parentOrResult = Directories[fullpath];
                }
                else
                {
                    Directory dir = new Directory(parts[i], parentOrResult);
                    Directories[fullpath] = dir;
                    parentOrResult = dir;
                }
            }

            return parentOrResult;
        }

        private void AddExpansion(LibraryItem parent, LibraryItem child)
        {
            IDictionary<LibraryItem, ISet<LibraryItem>> lookup = null;

            if (parent is Artist)
            {
                lookup = m_ArtistExpansion;
            }
            else if (parent is Album)
            {
                lookup = m_AlbumExpansion;
            }
            else if (parent is Genre)
            {
                lookup = m_GenreExpansion;
            }
            else if (parent is GenreFilteredAlbum)
            {
                lookup = m_GenreFilteredAlbumExpansion;
            }
            else if (parent is Directory)
            {
                lookup = m_DirectoryExpansion;
            }

            if (lookup == null)
            {
                throw new Exception("");
            }

            if (!lookup.ContainsKey(parent))
            {
                lookup[parent] = new SortedSet<LibraryItem>();
            }

            lookup[parent].Add(child);
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
