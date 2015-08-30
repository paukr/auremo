﻿/*
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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

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
            AlbumsBySelectedArtists.CreateFrom(m_DataModel.Database.Expand(Artists.SelectedItems()));
            NotifyPropertyChanged("AlbumsBySelectedArtists");
        }

        public void OnSelectedAlbumsBySelectedArtistsChanged()
        {
            SongsOnSelectedAlbumsBySelectedArtists.CreateFrom(m_DataModel.Database.Expand(AlbumsBySelectedArtists.SelectedItems()));
            NotifyPropertyChanged("SongsOnSelectedAlbumsBySelectedArtists");
        }

        public void ShowInArtistList(IEnumerable<Playable> playables)
        {
            ISet<Path> paths = new SortedSet<Path>(playables.Select(e => e.Path));
            ISet<Song> songs = new SortedSet<Song>(paths.Where(e => m_DataModel.Database.Songs.ContainsKey(e)).Select(e => m_DataModel.Database.Songs[e]));
            ISet<Album> albums = new SortedSet<Album>(songs.Where(e => e.Album != null).Select(e => e.Album));
            ISet<Artist> artists = new SortedSet<Artist>(albums.Where(e => e.Artist != null).Select(e => e.Artist));

            foreach (IndexedLibraryItem row in Artists)
            {
                row.IsSelected = artists.Contains(row.Item as Artist);
            }

            OnSelectedArtistsChanged();

            foreach (IndexedLibraryItem row in AlbumsBySelectedArtists)
            {
                row.IsSelected = albums.Contains(row.Item as Album);
            }

            OnSelectedAlbumsBySelectedArtistsChanged();

            foreach (IndexedLibraryItem row in SongsOnSelectedAlbumsBySelectedArtists)
            {
                row.IsSelected = songs.Contains(row.Item as Song);
            }
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
            AlbumsOfSelectedGenres.CreateFrom(m_DataModel.Database.Expand(Genres.SelectedItems()));
            NotifyPropertyChanged("AlbumsOfSelectedGenres");
        }

        public void OnSelectedAlbumsOfSelectedGenresChanged()
        {
            SongsOnSelectedAlbumsOfSelectedGenres.CreateFrom(m_DataModel.Database.Expand(AlbumsOfSelectedGenres.SelectedItems()));
            NotifyPropertyChanged("SongsOnSelectedAlbumsOfSelectedGenres");
        }

        public void ShowSongsInGenreList(IEnumerable<SongMetadata> selectedSongs)
        {
            /*
            foreach (MusicCollectionItem genreItem in Genres)
            {
                genreItem.IsSelected = false;

                foreach (SongMetadata selectedSong in selectedSongs)
                {
                    if (genreItem.Content as string == selectedSong.Genre)
                    {
                        genreItem.IsSelected = true;
                    }
                }
            }
            
            OnSelectedGenresChanged();

            foreach (MusicCollectionItem albumItem in AlbumsOfSelectedGenres)
            {
                albumItem.IsSelected = false;
                AlbumMetadata album = albumItem.Content as AlbumMetadata;

                foreach (SongMetadata selectedSong in selectedSongs)
                {
                    if (album.Artist == selectedSong.Artist && album.Title == selectedSong.Album)
                    {
                        albumItem.IsSelected = true;
                    }
                }
            }
            
            OnSelectedAlbumsOfSelectedGenresChanged();

            foreach (MusicCollectionItem songItem in SongsOnSelectedAlbumsOfSelectedGenres)
            {
                songItem.IsSelected = false;
                SongMetadata songInView = songItem.Content as SongMetadata;

                foreach (SongMetadata selectedSong in selectedSongs)
                {
                    if (songInView.Path == selectedSong.Path)
                    {
                        songItem.IsSelected = true;
                    }
                }
            }
            */ 
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
                new HierarchicalLibraryItem(song, directoryLookup[song.Directory]);
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

        #endregion

        #region Old artist/album/song tree view

        public void ShowSongsInArtistTree(IEnumerable<SongMetadata> selectedSongs)
        {
            /*
            ISet<string> selectedArtists = new SortedSet<string>();
            ISet<AlbumMetadata> selectedAlbums = new SortedSet<AlbumMetadata>();
            ISet<string> selectedSongPaths = new SortedSet<string>(StringComparer.Ordinal);

            foreach (SongMetadata song in selectedSongs)
            {
                if (song.IsLocal)
                {
                    selectedArtists.Add(song.Artist);
                    selectedAlbums.Add(new AlbumMetadata(song.Artist, song.Album, null));
                    selectedSongPaths.Add(song.Path);
                }
            }

            OldArtistTreeController.ClearMultiSelection();

            foreach (TreeViewNode rootNode in OldArtistTreeController.RootLevelNodes)
            {
                ArtistTreeViewNode artistNode = rootNode as ArtistTreeViewNode;
                artistNode.IsExpanded = false;

                if (selectedArtists.Contains(artistNode.Artist))
                {
                    artistNode.IsExpanded = true;

                    foreach (TreeViewNode midNode in artistNode.Children)
                    {
                        AlbumMetadataTreeViewNode albumNode = midNode as AlbumMetadataTreeViewNode;
                        albumNode.IsExpanded = false;

                        if (selectedAlbums.Contains(albumNode.Album))
                        {
                            albumNode.IsExpanded = true;

                            foreach (TreeViewNode leafNode in albumNode.Children)
                            {
                                SongMetadataTreeViewNode songNode = leafNode as SongMetadataTreeViewNode;

                                if (selectedSongPaths.Contains(songNode.Song.Path))
                                {
                                    songNode.IsMultiSelected = true;
                                }
                            }
                        }
                    }
                }
            }
            */ 
        }

        #endregion

        #region Genre/album/song tree view

        

        public void ShowSongsInGenreTree(IEnumerable<SongMetadata> selectedSongs)
        {
            /*
            ISet<string> selectedGenres = new SortedSet<string>();
            ISet<AlbumMetadata> selectedAlbums = new SortedSet<AlbumMetadata>();
            ISet<string> selectedSongPaths = new SortedSet<string>(StringComparer.Ordinal);

            foreach (SongMetadata song in selectedSongs)
            {
                if (song.IsLocal)
                {
                    selectedGenres.Add(song.Genre);
                    selectedAlbums.Add(new AlbumMetadata(song.Artist, song.Album, null));
                    selectedSongPaths.Add(song.Path);
                }
            }

            OldGenreTreeController.ClearMultiSelection();

            foreach (TreeViewNode rootNode in OldGenreTreeController.RootLevelNodes)
            {
                GenreTreeViewNode genreNode = rootNode as GenreTreeViewNode;
                genreNode.IsExpanded = false;

                if (selectedGenres.Contains(genreNode.Genre))
                {
                    genreNode.IsExpanded = true;

                    foreach (TreeViewNode midNode in genreNode.Children)
                    {
                        AlbumMetadataTreeViewNode albumNode = midNode as AlbumMetadataTreeViewNode;
                        albumNode.IsExpanded = false;

                        if (selectedAlbums.Contains(albumNode.Album))
                        {
                            albumNode.IsExpanded = true;

                            foreach (TreeViewNode leafNode in albumNode.Children)
                            {
                                SongMetadataTreeViewNode songNode = leafNode as SongMetadataTreeViewNode;

                                if (selectedSongPaths.Contains(songNode.Song.Path))
                                {
                                    songNode.IsMultiSelected = true;
                                }
                            }
                        }
                    }
                }
            }
            */ 
        }

        #endregion
        
        #region Directory tree view

        public void ShowSongsInDirectoryTree(IEnumerable<SongMetadata> selectedSongs)
        {
            /*
            DirectoryTreeController.ClearMultiSelection();

            // This looks more complex than necessary because it is trying to
            // support multiple roots.
            foreach (TreeViewNode root in DirectoryTreeController.RootLevelNodes)
            {
                if (root is DirectoryTreeViewNode)
                {
                    DirectoryTreeViewNode rootDirectory = root as DirectoryTreeViewNode;
                    
                    foreach (TreeViewNode node in rootDirectory.Children)
                    {
                        node.IsExpanded = false;

                        foreach (SongMetadata song in selectedSongs)
                        {
                            SearchAndSelectPath(node, song.Path);
                        }
                    }
                }
            }
            */ 
        }
        
        // Expand/multiselect node if the path is found under it.
        private bool SearchAndSelectPath(HierarchicalLibraryItem node, string path)
        {
            /*
            if (node is DirectoryTreeViewNode)
            {
                DirectoryTreeViewNode directory = node as DirectoryTreeViewNode;

                if (path.StartsWith(directory.FullPath + "/"))
                {
                    foreach (TreeViewNode child in directory.Children)
                    {
                        bool found = SearchAndSelectPath(child, path);

                        if (found)
                        {
                            directory.IsExpanded = true;
                            return true;
                        }
                    }
                }
            }
            else if (node is SongMetadataTreeViewNode)
            {
                SongMetadataTreeViewNode song = node as SongMetadataTreeViewNode;

                if (song.Song.Path == path)
                {
                    song.IsMultiSelected = true;
                    return true;
                }
            }
            */
            return false;
        }

        #endregion
    }
}
