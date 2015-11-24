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
using Auremo.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

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
        private IComparer<Album> m_AlbumSortRule = null;
        private IComparer<GenreFilteredAlbum> m_GenreFilteredAlbumSortRule = null;

        private IDictionary<Artist, IDictionary<string, Album>> m_AlbumLookup = new SortedDictionary<Artist, IDictionary<string, Album>>();
        private IDictionary<Genre, IDictionary<Album, GenreFilteredAlbum>> m_GenreFilteredAlbumLookup = new SortedDictionary<Genre, IDictionary<Album, GenreFilteredAlbum>>();

        private IDictionary<LibraryItem, ISet<Album>> m_ArtistExpansion = null;
        private IDictionary<LibraryItem, ISet<Song>> m_AlbumExpansion = null;
        private IDictionary<LibraryItem, ISet<GenreFilteredAlbum>> m_GenreExpansion = null;
        private IDictionary<LibraryItem, ISet<Song>> m_GenreFilteredAlbumExpansion = null;
        private IDictionary<LibraryItem, ISet<LibraryItem>> m_DirectoryExpansion = null;
        
        private string UnknownArtist = "Unknown Artist";
        private string UnknownGenre = "Unknown Genre";
        private string UnknownAlbum = "Unknown Album";

        public Database(DataModel dataModel)
        {
            m_DataModel = dataModel;

            Artists = new SortedDictionary<string, Artist>();
            Genres = new SortedDictionary<string, Genre>();
            Directories = new SortedDictionary<string, Directory>();
            Songs = new SortedDictionary<Path, Song>();

            m_DataModel.ServerSession.PropertyChanged += new PropertyChangedEventHandler(OnServerSessionPropertyChanged);
            m_DataModel.ServerStatus.PropertyChanged += new PropertyChangedEventHandler(OnServerStatusPropertyChanged);

            ProcessSettings();
            ClearCollection();
        }

        public void ClearCollection()
        {
            Artists.Clear();
            Genres.Clear();
            Directories.Clear();
            Songs.Clear();

            m_AlbumLookup.Clear();
            m_GenreFilteredAlbumLookup.Clear();

            m_ArtistExpansion = new SortedDictionary<LibraryItem, ISet<Album>>();
            m_AlbumExpansion = new SortedDictionary<LibraryItem, ISet<Song>>();
            m_GenreExpansion = new SortedDictionary<LibraryItem, ISet<GenreFilteredAlbum>>();
            m_GenreFilteredAlbumExpansion = new SortedDictionary<LibraryItem, ISet<Song>>();
            m_DirectoryExpansion = new SortedDictionary<LibraryItem, ISet<LibraryItem>>();

            m_ArtistExpansion.Clear();
            m_AlbumExpansion.Clear();
            m_GenreExpansion.Clear();
            m_GenreFilteredAlbumExpansion.Clear();

            NotifyPropertyChanged("Database");
        }

        private void RefreshCollection()
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
        
        public IEnumerable<LibraryItem> Expand(LibraryItem parent)
        {
            if (parent is Artist && m_ArtistExpansion.ContainsKey(parent))
            {
                return m_ArtistExpansion[parent].ToList();
            }
            else if (parent is Album && m_AlbumExpansion.ContainsKey(parent))
            {
                return m_AlbumExpansion[parent].ToList();
            }
            else if (parent is Genre && m_GenreExpansion.ContainsKey(parent))
            {
                return m_GenreExpansion[parent].ToList();
            }
            else if (parent is GenreFilteredAlbum && m_GenreFilteredAlbumExpansion.ContainsKey(parent))
            {
                return m_GenreFilteredAlbumExpansion[parent].ToList();
            }

            throw new Exception("Database.Expand(): cannot expand object: " + parent.ToString());
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
                m_AlbumSortRule = new AlbumByDateComparer();
                m_GenreFilteredAlbumSortRule = new GenreFilteredAlbumByDateComparer();
            }
            else if (Settings.Default.AlbumSortingMode == AlbumSortingMode.ByDirectory.ToString())
            {
                m_AlbumSortRule = new AlbumByDirectoryComparer();
                m_GenreFilteredAlbumSortRule = new GenreFilteredAlbumByDirectoryComparer();
            }
            else
            {
                m_AlbumSortRule = new AlbumByTitleComparer();
                m_GenreFilteredAlbumSortRule = new GenreFilteredAlbumByTitleComparer();
            }
        }

        private void QuerySongInfo()
        {
            m_DataModel.ServerSession.Send(MPDCommandFactory.ListAllInfo());
        }

        private void PopulateDatabase(IEnumerable<MPDSongResponseBlock> response)
        {
            foreach (MPDSongResponseBlock block in response)
            {
                Song song = new Song(block);

                song.Date = song.IsSpotify ?
                    m_DataModel.YearNormalizer.Normalize(block.Date) :
                    m_DataModel.CustomDateNormalizer.Normalize(block.Date);

                song.Artist = GetOrCreateArtist(SelectArtistTag(block));
                song.Genre = GetOrCreateGenre(block.Genre);
                song.Album = GetOrCreateAlbum(block);
                song.GenreFilteredAlbum = GetOrCreateGenreFilteredAlbum(song.Album, block);
                song.Directory = GetOrCreateDirectory(song.Path);

                Songs[song.Path] = song;
                AddAlbumExpansion(song.Album, song);
                AddGenreFilteredAlbumExpansion(song.GenreFilteredAlbum, song);
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

        private Album GetOrCreateAlbum(MPDSongResponseBlock block)
        {
            Artist artist = Artists[SelectArtistTag(block)];
            string albumKey = block.Album ?? UnknownAlbum;

            if (!m_AlbumLookup.ContainsKey(artist))
            {
                m_AlbumLookup[artist] = new SortedDictionary<string, Album>(StringComparer.Ordinal);
            }

            IDictionary<string, Album> albumList = m_AlbumLookup[artist];

            if (!albumList.ContainsKey(albumKey))
            {
                string date = null;

                if (block.Date != null)
                {
                    if (Path.IsSpotify(block.File))
                    {
                        date = m_DataModel.YearNormalizer.Normalize(block.Date);
                    }
                    else
                    {
                        date = m_DataModel.CustomDateNormalizer.Normalize(block.Date);
                    }
                }

                string directory = new Path(block.File).Directories.Last();
                Album album = new Album(artist, albumKey, date, directory);
                albumList[albumKey] = album;
                AddArtistExpansion(artist, album);
            }

            return albumList[albumKey];
        }

        private GenreFilteredAlbum GetOrCreateGenreFilteredAlbum(Album album, MPDSongResponseBlock block)
        {
            Genre genre = Genres[block.Genre ?? UnknownGenre];

            if (!m_GenreFilteredAlbumLookup.ContainsKey(genre))
            {
                m_GenreFilteredAlbumLookup[genre] = new SortedDictionary<Album, GenreFilteredAlbum>();
            }

            IDictionary<Album, GenreFilteredAlbum> albumList = m_GenreFilteredAlbumLookup[genre];

            if (!albumList.ContainsKey(album))
            {
                string directory = new Path(block.File).Directories.Last();
                GenreFilteredAlbum genreFilteredAlbum = new GenreFilteredAlbum(genre, album.Artist, album.Title, album.Date, directory);
                m_GenreFilteredAlbumLookup[genre][album] = genreFilteredAlbum;
                AddGenreExpansion(genre, genreFilteredAlbum);
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

        private string SelectArtistTag(MPDSongResponseBlock block)
        {
            if (Settings.Default.UseAlbumArtist)
            {
                return block.AlbumArtist ?? block.Artist ?? UnknownArtist;
            }
            else
            {
                return block.Artist ?? UnknownArtist;
            }
        }

        private void AddArtistExpansion(Artist parent, Album child)
        {
            if (!m_ArtistExpansion.ContainsKey(parent))
            {
                m_ArtistExpansion[parent] = new SortedSet<Album>(m_AlbumSortRule);
            }

            m_ArtistExpansion[parent].Add(child);
        }

        private void AddAlbumExpansion(Album parent, Song child)
        {
            if (!m_AlbumExpansion.ContainsKey(parent))
            {
                m_AlbumExpansion[parent] = new SortedSet<Song>();
            }

            m_AlbumExpansion[parent].Add(child);
        }

        private void AddGenreExpansion(Genre parent, GenreFilteredAlbum child)
        {
            if (!m_GenreExpansion.ContainsKey(parent))
            {
                m_GenreExpansion[parent] = new SortedSet<GenreFilteredAlbum>(m_GenreFilteredAlbumSortRule);
            }

            m_GenreExpansion[parent].Add(child);
        }

        private void AddGenreFilteredAlbumExpansion(GenreFilteredAlbum parent, Song child)
        {
            if (!m_GenreFilteredAlbumExpansion.ContainsKey(parent))
            {
                m_GenreFilteredAlbumExpansion[parent] = new SortedSet<Song>();
            }

            m_GenreFilteredAlbumExpansion[parent].Add(child);
        }

        private void OnServerSessionPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "State")
            {
                if (m_DataModel.ServerSession.State != ServerSession.SessionState.Connected)
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
