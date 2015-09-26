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
using System.Windows.Threading;

namespace Auremo
{
    public class DatabaseView : INotifyPropertyChanged
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

        #region Construction and setup

        public DatabaseView(DataModel dataModel)
        {
            m_DataModel = dataModel;

            Artists = new ObservableCollection<IndexedLibraryItem>();
            AlbumsBySelectedArtists = new ObservableCollection<IndexedLibraryItem>();
            SongsOnSelectedAlbumsBySelectedArtists = new ObservableCollection<IndexedLibraryItem>();

            Genres = new ObservableCollection<IndexedLibraryItem>();
            AlbumsOfSelectedGenres = new ObservableCollection<IndexedLibraryItem>();
            SongsOnSelectedAlbumsOfSelectedGenres = new ObservableCollection<IndexedLibraryItem>();

            ArtistTree = new ObservableCollection<HierarchicalLibraryItem>();
            ArtistTreeController = new HierarchyController(ArtistTree);

            GenreTree = new ObservableCollection<HierarchicalLibraryItem>();
            GenreTreeController = new HierarchyController(GenreTree);

            DirectoryTree = new ObservableCollection<HierarchicalLibraryItem>();
            DirectoryTreeController = new HierarchyController(DirectoryTree);

            m_DataModel.Database.PropertyChanged += new PropertyChangedEventHandler(OnDatabasePropertyChanged);
        }

        private void OnDatabasePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Database")
            {
                Artists.CreateFrom(m_DataModel.Database.Artists.Values);
                Genres.CreateFrom(m_DataModel.Database.Genres.Values);
                PopulateArtistTree();
                PopulateGenreTree();
                PopulateDirectoryTree();
            }
        }
        
        #endregion

        #region Artist/album/song view

        public ObservableCollection<IndexedLibraryItem> Artists
        {
            get;
            private set;
        }

        public ObservableCollection<IndexedLibraryItem> AlbumsBySelectedArtists
        {
            get;
            private set;
        }

        public ObservableCollection<IndexedLibraryItem> SongsOnSelectedAlbumsBySelectedArtists
        {
            get;
            private set;
        }

        public void OnSelectedArtistsChanged()
        {
            AlbumsBySelectedArtists.CreateFrom(m_DataModel.Database.Expand(SelectedArtists));
            NotifyPropertyChanged("SelectedArtists");
        }

        public void OnSelectedAlbumsBySelectedArtistsChanged()
        {
            SongsOnSelectedAlbumsBySelectedArtists.CreateFrom(m_DataModel.Database.Expand(SelectedAlbumsBySelectedArtists));
            NotifyPropertyChanged("SelectedAlbumsBySelectedArtists");
        }

        public void OnSelectedSongsOnSelectedAlbumsBySelectedArtistsChanged()
        {
            NotifyPropertyChanged("SelectedSongsOnSelectedAlbumsBySelectedArtists");
        }

        public void ShowInArtistList(IEnumerable<Playable> playables)
        {
            ISet<Path> paths = new SortedSet<Path>(playables.Select(e => e.Path));
            ISet<Song> songs = new SortedSet<Song>(paths.Where(e => m_DataModel.Database.Songs.ContainsKey(e)).Select(e => m_DataModel.Database.Songs[e]));
            ISet<Album> albums = new SortedSet<Album>(songs.Where(e => e.Album != null).Select(e => e.Album));
            ISet<Artist> artists = new SortedSet<Artist>(albums.Where(e => e.Artist != null).Select(e => e.Artist));

            foreach (IndexedLibraryItem row in Artists)
            {
                row.IsSelected = artists.Contains(row.ItemAs<Artist>());
            }

            m_DataModel.MainWindow.Dispatcher.BeginInvoke(new Action(() =>
            {
                foreach (IndexedLibraryItem row in AlbumsBySelectedArtists)
                {
                    row.IsSelected = albums.Contains(row.ItemAs<Album>());
                }

                m_DataModel.MainWindow.Dispatcher.BeginInvoke(new Action(() =>
                {
                    foreach (IndexedLibraryItem row in SongsOnSelectedAlbumsBySelectedArtists)
                    {
                        row.IsSelected = songs.Contains(row.ItemAs<Song>());
                    }

                    OnSelectedSongsOnSelectedAlbumsBySelectedArtistsChanged();
                }), DispatcherPriority.Loaded);
            }), DispatcherPriority.Loaded);
        }

        #endregion

        #region Genre/album/artist view

        public ObservableCollection<IndexedLibraryItem> Genres
        {
            get;
            private set;
        }

        public ObservableCollection<IndexedLibraryItem> AlbumsOfSelectedGenres
        {
            get;
            private set;
        }

        public ObservableCollection<IndexedLibraryItem> SongsOnSelectedAlbumsOfSelectedGenres
        {
            get;
            private set;
        }
        
        public void OnSelectedGenresChanged()
        {
            AlbumsOfSelectedGenres.CreateFrom(m_DataModel.Database.Expand(SelectedGenres));
            NotifyPropertyChanged("SelectedGenres");
        }

        public void OnSelectedAlbumsOfSelectedGenresChanged()
        {
            SongsOnSelectedAlbumsOfSelectedGenres.CreateFrom(m_DataModel.Database.Expand(SelectedAlbumsOfSelectedGenres));
            NotifyPropertyChanged("SelectedAlbumsOfSelectedGenres");
        }

        public void OnSelectedSongsOnSelectedAlbumsOfSelectedGenresChanged()
        {
            NotifyPropertyChanged("SelectedSongsOnSelectedAlbumsOfSelectedGenres");
        }

        public void ShowInGenreList(IEnumerable<Playable> playables)
        {
            ISet<Path> paths = new SortedSet<Path>(playables.Select(e => e.Path));
            ISet<Song> songs = new SortedSet<Song>(paths.Where(e => m_DataModel.Database.Songs.ContainsKey(e)).Select(e => m_DataModel.Database.Songs[e]));
            ISet<GenreFilteredAlbum> albums = new SortedSet<GenreFilteredAlbum>(songs.Where(e => e.GenreFilteredAlbum != null).Select(e => e.GenreFilteredAlbum));
            ISet<Genre> genres = new SortedSet<Genre>(albums.Where(e => e.Genre != null).Select(e => e.Genre));

            foreach (IndexedLibraryItem row in Genres)
            {
                row.IsSelected = genres.Contains(row.ItemAs<Genre>());
            }

            m_DataModel.MainWindow.Dispatcher.BeginInvoke(new Action(() =>
            {
                foreach (IndexedLibraryItem row in AlbumsOfSelectedGenres)
                {
                    row.IsSelected = albums.Contains(row.ItemAs<GenreFilteredAlbum>());
                }
                
                m_DataModel.MainWindow.Dispatcher.BeginInvoke(new Action(() =>
                {
                    foreach (IndexedLibraryItem row in SongsOnSelectedAlbumsOfSelectedGenres)
                    {
                        row.IsSelected = songs.Contains(row.ItemAs<Song>());
                    }

                    OnSelectedSongsOnSelectedAlbumsOfSelectedGenresChanged();
                }), DispatcherPriority.Loaded);
            }), DispatcherPriority.Loaded);
        }
        
        #endregion

        #region Artist/album/song tree view

        public ObservableCollection<HierarchicalLibraryItem> ArtistTree
        {
            get;
            private set;
        }

        public HierarchyController ArtistTreeController
        {
            get;
            private set;
        }

        private void PopulateArtistTree()
        {
            ArtistTreeController.Clear();
            ArtistTree.Clear();

            foreach (Artist artist in m_DataModel.Database.Artists.Values)
            {
                HierarchicalLibraryItem artistNode = new HierarchicalLibraryItem(artist, ArtistTreeController);
                ArtistTree.Add(artistNode);

                foreach (LibraryItem album in m_DataModel.Database.Expand(artist))
                {
                    HierarchicalLibraryItem albumNode = new HierarchicalLibraryItem(album, artistNode);

                    foreach (LibraryItem song in m_DataModel.Database.Expand(album))
                    {
                        new HierarchicalLibraryItem(song, albumNode);
                    }
                }
            }

            ArtistTreeController.ResetNodeIds();
        }

        public void ShowInArtistTree(IEnumerable<Playable> playables)
        {
            ISet<Path> paths = new SortedSet<Path>(playables.Select(e => e.Path));
            ArtistTreeController.ClearMultiSelection();

            foreach (HierarchicalLibraryItem item in ArtistTreeController.LeafNodes)
            {
                if (paths.Contains((item.Item as Playable).Path))
                {
                    item.IsMultiSelected = true;
                }
            }
        }

        #endregion

        #region Genre/album/song tree view

        public ObservableCollection<HierarchicalLibraryItem> GenreTree
        {
            get;
            private set;
        }

        public HierarchyController GenreTreeController
        {
            get;
            private set;
        }

        private void PopulateGenreTree()
        {
            GenreTreeController.Clear();
            GenreTree.Clear();

            foreach (Genre genre in m_DataModel.Database.Genres.Values)
            {
                HierarchicalLibraryItem genreNode = new HierarchicalLibraryItem(genre, GenreTreeController);
                GenreTree.Add(genreNode);

                foreach (LibraryItem album in m_DataModel.Database.Expand(genre))
                {
                    HierarchicalLibraryItem albumNode = new HierarchicalLibraryItem(album, genreNode);

                    foreach (LibraryItem song in m_DataModel.Database.Expand(album))
                    {
                        new HierarchicalLibraryItem(song, albumNode);
                    }
                }
            }

            GenreTreeController.ResetNodeIds();
        }

        public void ShowInGenreTree(IEnumerable<Playable> playables)
        {
            ISet<Path> paths = new SortedSet<Path>(playables.Select(e => e.Path));
            GenreTreeController.ClearMultiSelection();

            foreach (HierarchicalLibraryItem item in GenreTreeController.LeafNodes)
            {
                if (paths.Contains((item.Item as Playable).Path))
                {
                    item.IsMultiSelected = true;
                }
            }
        }

        #endregion

        #region Directory tree

        public ObservableCollection<HierarchicalLibraryItem> DirectoryTree
        {
            get;
            private set;
        }

        public HierarchyController DirectoryTreeController
        {
            get;
            private set;
        }

        private void PopulateDirectoryTree()
        {
            DirectoryTreeController.Clear();
            DirectoryTree.Clear();

            IDictionary<Directory, HierarchicalLibraryItem> directoryLookup = new SortedDictionary<Directory, HierarchicalLibraryItem>();

            foreach (Directory directory in m_DataModel.Database.Directories.Values)
            {
                CreateDirectoryBranchNodesRecursively(directory, directoryLookup);
            }

            foreach (Song song in m_DataModel.Database.Songs.Values)
            {
                if (song.Directory == null)
                {
                    DirectoryTree.Add(new HierarchicalLibraryItem(song, DirectoryTreeController));
                }
                else
                {
                    new HierarchicalLibraryItem(song, directoryLookup[song.Directory]);
                }
            }

            DirectoryTreeController.ResetNodeIds();
        }

        private HierarchicalLibraryItem CreateDirectoryBranchNodesRecursively(Directory directory, IDictionary<Directory, HierarchicalLibraryItem> directoryLookup)
        {
            HierarchicalLibraryItem result = null;

            if (directory.Parent == null)
            {
                result = new HierarchicalLibraryItem(directory, DirectoryTreeController);
                DirectoryTree.Add(result);
            }
            else if (directoryLookup.ContainsKey(directory.Parent))
            {
                result = new HierarchicalLibraryItem(directory, directoryLookup[directory.Parent]);
            }
            else
            {
                result = new HierarchicalLibraryItem(directory, CreateDirectoryBranchNodesRecursively(directory.Parent, directoryLookup));
            }

            directoryLookup[directory] = result;
            return result;
        }

        public void ShowInDirectoryTree(IEnumerable<Playable> playables)
        {
            ISet<Path> paths = new SortedSet<Path>(playables.Select(e => e.Path));
            DirectoryTreeController.ClearMultiSelection();

            foreach (HierarchicalLibraryItem item in DirectoryTreeController.LeafNodes)
            {
                if (paths.Contains((item.Item as Playable).Path))
                {
                    item.IsMultiSelected = true;
                }
            }
        }

        #endregion

        #region Simple declarative properties

        public IEnumerable<LibraryItem> SelectedArtists => Artists.SelectedItems();
        public IEnumerable<LibraryItem> SelectedAlbumsBySelectedArtists => AlbumsBySelectedArtists.SelectedItems();
        public IEnumerable<LibraryItem> SelectedSongsOnSelectedAlbumsBySelectedArtists => SongsOnSelectedAlbumsBySelectedArtists.SelectedItems();

        public IEnumerable<LibraryItem> SelectedGenres => Genres.SelectedItems();
        public IEnumerable<LibraryItem> SelectedAlbumsOfSelectedGenres => AlbumsOfSelectedGenres.SelectedItems();
        public IEnumerable<LibraryItem> SelectedSongsOnSelectedAlbumsOfSelectedGenres => SongsOnSelectedAlbumsOfSelectedGenres.SelectedItems();

        #endregion
    }
}
