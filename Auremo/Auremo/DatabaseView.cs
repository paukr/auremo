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

        //private ISet<string> m_SelectedGenres = new SortedSet<string>();
        //private ISet<AlbumMetadata> m_SelectedAlbumsOfSelectedGenres = new SortedSet<AlbumMetadata>();

        //public delegate ISet<AlbumMetadata> AlbumsUnderRoot(string root);
        //public delegate ISet<SongMetadata> SongsOnAlbum(AlbumMetadata album);

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

            /*
            OldArtistTree = new ObservableCollection<TreeViewNode>();
            OldArtistTreeController = new TreeViewController(OldArtistTree);

            OldGenreTree = new ObservableCollection<TreeViewNode>();
            OldGenreTreeController = new TreeViewController(OldGenreTree);

            OldDirectoryTree = new ObservableCollection<TreeViewNode>();
            OldDirectoryTreeController = new TreeViewController(OldDirectoryTree);
            */ 

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

                return;

                /*
                AlbumsBySelectedArtistsTODO.Clear();
                SongsOnSelectedAlbumsBySelectedArtists.Clear();
                PopulateGenres();
                AlbumsOfSelectedGenres.Clear();
                SongsOnSelectedAlbumsOfSelectedGenres.Clear();
                PopulateDirectoryTree();
                PopulateArtistTree();
                PopulateGenreTree();
                */ 
            }
        }
        
        /*
        private void PopulateGenreTree()
        {
            OldGenreTreeController.ClearMultiSelection();
            OldGenreTree.Clear();
            
            foreach (string genre in m_DataModel.Database.Genres)
            {
                GenreTreeViewNode genreNode = new GenreTreeViewNode(genre, null, GenreTreeController);

                foreach (AlbumMetadata album in m_DataModel.Database.AlbumsByGenre(genre))
                {
                    AlbumMetadataTreeViewNode albumNode = new AlbumMetadataTreeViewNode(album, genreNode, GenreTreeController);
                    genreNode.AddChild(albumNode);

                    foreach (SongMetadata song in m_DataModel.Database.SongsByAlbum(album))
                    {
                        if (song.Genre == genre)
                        {
                            SongMetadataTreeViewNode songNode = new SongMetadataTreeViewNode("", song, albumNode, GenreTreeController);
                            albumNode.AddChild(songNode);
                        }
                    }
                }

                GenreTree.Add(genreNode);
            }

            int id = 0;

            foreach (TreeViewNode baseNode in GenreTree)
            {
                id = AssignTreeViewNodeIDs(baseNode, id);
            }
            
        }
        */
        /*
         private void PopulateDirectoryTree()
         {
             DirectoryTreeController.ClearMultiSelection();
             DirectoryTree.Clear();

             DirectoryTreeViewNode rootNode = new DirectoryTreeViewNode("/", null, DirectoryTreeController);
             IDictionary<string, TreeViewNode> directoryLookup = new SortedDictionary<string, TreeViewNode>();
             directoryLookup[rootNode.DisplayString] = rootNode;

             foreach (SongMetadata song in m_DataModel.Database.Songs)
             {
                 TreeViewNode parent = FindDirectoryNode(song.Directory, directoryLookup, rootNode);
                 SongMetadataTreeViewNode leaf = new SongMetadataTreeViewNode(song.Filename, song, parent, DirectoryTreeController);
                 parent.AddChild(leaf);
             }

             AssignTreeViewNodeIDs(rootNode, 0);
            
             if (rootNode.Children.Count > 0)
             {
                 DirectoryTree.Add(rootNode);
                 rootNode.IsExpanded = true;
             }

         }
         */

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

        public void ShowSongsInArtistList(IEnumerable<SongMetadata> selectedSongs)
        {
            /*
            foreach (MusicCollectionItem artistItem in ArtistsTODO)
            {
                artistItem.IsSelected = false;

                foreach (SongMetadata selectedSong in selectedSongs)
                {
                    if (artistItem.Content as string == selectedSong.Artist)
                    {
                        artistItem.IsSelected = true;
                    }
                }
            }

            OnSelectedArtistsChanged();

            foreach (MusicCollectionItem albumItem in AlbumsBySelectedArtistsTODO)
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

            OnSelectedAlbumsBySelectedArtistsChanged();

            foreach (MusicCollectionItem songItem in SongsOnSelectedAlbumsBySelectedArtists)
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
