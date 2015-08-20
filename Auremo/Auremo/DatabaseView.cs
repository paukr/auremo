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

        private ISet<string> m_SelectedGenres = new SortedSet<string>();
        private ISet<AlbumMetadata> m_SelectedAlbumsOfSelectedGenres = new SortedSet<AlbumMetadata>();

        public delegate ISet<AlbumMetadata> AlbumsUnderRoot(string root);
        public delegate ISet<SongMetadata> SongsOnAlbum(AlbumMetadata album);

        #region Construction and setup

        public DatabaseView(DataModel dataModel)
        {
            m_DataModel = dataModel;

            Artists = new ObservableCollection<MusicCollectionItem>();
            AlbumsBySelectedArtists = new ObservableCollection<MusicCollectionItem>();
            SongsOnSelectedAlbumsBySelectedArtists = new ObservableCollection<MusicCollectionItem>();

            Genres = new ObservableCollection<MusicCollectionItem>();
            AlbumsOfSelectedGenres = new ObservableCollection<MusicCollectionItem>();
            SongsOnSelectedAlbumsOfSelectedGenres = new ObservableCollection<MusicCollectionItem>();

            ArtistTree = new ObservableCollection<TreeViewNode>();
            ArtistTreeController = new TreeViewController(ArtistTree);

            GenreTree = new ObservableCollection<TreeViewNode>();
            GenreTreeController = new TreeViewController(GenreTree);

            DirectoryTree = new ObservableCollection<TreeViewNode>();
            DirectoryTreeController = new TreeViewController(DirectoryTree);

            m_DataModel.Database.PropertyChanged += new PropertyChangedEventHandler(OnDatabasePropertyChanged);
        }

        private void OnDatabasePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Database")
            {
                PopulateArtists();
                AlbumsBySelectedArtists.Clear();
                SongsOnSelectedAlbumsBySelectedArtists.Clear();
                PopulateGenres();
                AlbumsOfSelectedGenres.Clear();
                SongsOnSelectedAlbumsOfSelectedGenres.Clear();
                PopulateDirectoryTree();
                PopulateArtistTree();
                PopulateGenreTree();
            }
        }

        private void PopulateArtists()
        {
            Artists.Clear();

            foreach (string artist in m_DataModel.Database.Artists)
            {
                Artists.Add(new MusicCollectionItem(artist, Artists.Count));
            }
        }

        private void PopulateGenres()
        {
            Genres.Clear();

            foreach (string genre in m_DataModel.Database.Genres)
            {
                Genres.Add(new MusicCollectionItem(genre, Genres.Count));
            }
        }
        
        private void PopulateArtistTree()
        {
            ArtistTree.Clear();
            ArtistTreeController.MultiSelection.Clear();

            foreach (string artist in m_DataModel.Database.Artists)
            {
                ArtistTreeViewNode artistNode = new ArtistTreeViewNode(artist, null, ArtistTreeController);

                foreach (AlbumMetadata album in m_DataModel.Database.AlbumsByArtist(artist))
                {
                    AlbumMetadataTreeViewNode albumNode = new AlbumMetadataTreeViewNode(album, artistNode, ArtistTreeController);
                    artistNode.AddChild(albumNode);

                    foreach (SongMetadata song in m_DataModel.Database.SongsByAlbum(album))
                    {
                        SongMetadataTreeViewNode songNode = new SongMetadataTreeViewNode("", song, albumNode, ArtistTreeController);
                        albumNode.AddChild(songNode);
                    }
                }

                ArtistTree.Add(artistNode); // Insert now that branch is fully populated.
            }

            int id = 0;

            foreach (TreeViewNode baseNode in ArtistTree)
            {
                id = AssignTreeViewNodeIDs(baseNode, id);
            }
        }

        private void PopulateGenreTree()
        {
            GenreTreeController.ClearMultiSelection();
            GenreTree.Clear();

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

        private TreeViewNode FindDirectoryNode(string path, IDictionary<string, TreeViewNode> lookup, TreeViewNode rootNode)
        {
            if (path == "")
            {
                return rootNode;
            }
            else if (lookup.ContainsKey(path))
            {
                return lookup[path];
            }
            else
            {
                Tuple<string, string> parentAndSelf = Utils.SplitPath(path);
                TreeViewNode parent = FindDirectoryNode(parentAndSelf.Item1, lookup, rootNode);
                TreeViewNode self = new DirectoryTreeViewNode(parentAndSelf.Item2, parent, DirectoryTreeController);
                parent.AddChild(self);
                lookup[path] = self;
                return self;
            }
        }

        private int AssignTreeViewNodeIDs(TreeViewNode node, int nodeID)
        {
            node.ID = nodeID;
            int nextNodeID = nodeID + 1;

            foreach (TreeViewNode child in node.Children)
            {
                nextNodeID = AssignTreeViewNodeIDs(child, nextNodeID);
            }

            node.HighestChildID = nextNodeID - 1;
            return nextNodeID;
        }

        #endregion

        #region Artist/album/song view

        public ObservableCollection<MusicCollectionItem> Artists
        {
            get;
            private set;
        }

        public ObservableCollection<MusicCollectionItem> AlbumsBySelectedArtists
        {
            get;
            private set;
        }

        public ObservableCollection<MusicCollectionItem> SongsOnSelectedAlbumsBySelectedArtists
        {
            get;
            private set;
        }

        public IEnumerable<string> SelectedArtists
        {
            get
            {
                return CollectSelectedElements<string>(Artists);
            }
        }

        public IEnumerable<AlbumMetadata> SelectedAlbumsBySelectedArtists
        {
            get
            {
                return CollectSelectedElements<AlbumMetadata>(AlbumsBySelectedArtists);
            }
        }

        public IEnumerable<SongMetadata> SelectedSongsOnSelectedAlbumsBySelectedArtists
        {
            get
            {
                return CollectSelectedElements<SongMetadata>(SongsOnSelectedAlbumsBySelectedArtists);
            }
        }

        public void OnSelectedArtistsChanged()
        {
            AlbumsBySelectedArtists.Clear();

            foreach (string artist in SelectedArtists)
            {
                foreach (AlbumMetadata album in m_DataModel.Database.AlbumsByArtist(artist))
                {
                    AlbumsBySelectedArtists.Add(new MusicCollectionItem(album, AlbumsBySelectedArtists.Count));
                }
            }

            NotifyPropertyChanged("SelectedArtists");
        }

        public void OnSelectedAlbumsBySelectedArtistsChanged()
        {
            SongsOnSelectedAlbumsBySelectedArtists.Clear();

            foreach (AlbumMetadata album in SelectedAlbumsBySelectedArtists)
            {
                foreach (SongMetadata song in m_DataModel.Database.SongsByAlbum(album))
                {
                    SongsOnSelectedAlbumsBySelectedArtists.Add(new MusicCollectionItem(song, SongsOnSelectedAlbumsBySelectedArtists.Count));
                }
            }

            NotifyPropertyChanged("SelectedAlbumsBySelectedArtists");
        }

        public void OnSelectedSongsOnSelectedAlbumsBySelectedArtistsChanged()
        {
            NotifyPropertyChanged("SelectedSongsOnSelectedAlbumsBySelectedArtists");
        }

        public void ShowSongsInArtistList(IEnumerable<SongMetadata> selectedSongs)
        {
            foreach (MusicCollectionItem artistItem in Artists)
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

            foreach (MusicCollectionItem albumItem in AlbumsBySelectedArtists)
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
        }

        #endregion

        #region Genre/album/artist view

        public IList<MusicCollectionItem> Genres
        {
            get;
            private set;
        }

        public ObservableCollection<MusicCollectionItem> AlbumsOfSelectedGenres
        {
            get;
            private set;
        }

        public ObservableCollection<MusicCollectionItem> SongsOnSelectedAlbumsOfSelectedGenres
        {
            get;
            private set;
        }

        public IEnumerable<string> SelectedGenres
        {
            get
            {
                return CollectSelectedElements<string>(Genres);
            }
        }

        public IEnumerable<AlbumMetadata> SelectedAlbumsOfSelectedGenres
        {
            get
            {
                return CollectSelectedElements<AlbumMetadata>(AlbumsOfSelectedGenres);
            }
        }

        public IEnumerable<SongMetadata> SelectedSongsOnSelectedAlbumsOfSelectedGenres
        {
            get
            {
                return CollectSelectedElements<SongMetadata>(SongsOnSelectedAlbumsOfSelectedGenres);
            }
        }

        public void OnSelectedGenresChanged()
        {
            IList<AlbumMetadata> albums = new List<AlbumMetadata>();

            foreach (string genre in SelectedGenres)
            {
                foreach (AlbumMetadata album in m_DataModel.Database.AlbumsByGenre(genre))
                {
                    albums.Add(album);
                }
            }

            AlbumsOfSelectedGenres.Clear();

            foreach (AlbumMetadata album in albums)
            {
                AlbumsOfSelectedGenres.Add(new MusicCollectionItem(album, AlbumsOfSelectedGenres.Count));
            }

            NotifyPropertyChanged("SelectedGenres");
        }

        public void OnSelectedAlbumsOfSelectedGenresChanged()
        {
            ISet<SongMetadata> songs = new SortedSet<SongMetadata>();
            ISet<string> genres = new SortedSet<string>(SelectedGenres);

            foreach (AlbumMetadata album in SelectedAlbumsOfSelectedGenres)
            {
                foreach (SongMetadata song in m_DataModel.Database.SongsByAlbum(album))
                {
                    if (genres.Contains(song.Genre))
                    {
                        songs.Add(song);
                    }
                }
            }

            SongsOnSelectedAlbumsOfSelectedGenres.Clear();

            foreach (SongMetadata song in songs)
            {
                SongsOnSelectedAlbumsOfSelectedGenres.Add(new MusicCollectionItem(song, SongsOnSelectedAlbumsOfSelectedGenres.Count));
            }

            NotifyPropertyChanged("SelectedAlbumsOfSelectedGenres");
        }

        public void OnSelectedSongsOnSelectedAlbumsOfSelectedGenresChanged()
        {
            NotifyPropertyChanged("SelectedSongsOnSelectedAlbumsOfSelectedGenres");
        }

        public void ShowSongsInGenreList(IEnumerable<SongMetadata> selectedSongs)
        {
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
        }

        #endregion

        #region Artist/album/song tree view

        public IList<TreeViewNode> ArtistTree
        {
            get;
            private set;
        }

        public TreeViewController ArtistTreeController
        {
            get;
            private set;
        }

        public ISet<SongMetadataTreeViewNode> ArtistTreeSelectedSongs
        {
            get
            {
                return ArtistTreeController.Songs;
            }
        }

        public void ShowSongsInArtistTree(IEnumerable<SongMetadata> selectedSongs)
        {
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

            ArtistTreeController.ClearMultiSelection();

            foreach (TreeViewNode rootNode in ArtistTreeController.RootLevelNodes)
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
        }

        #endregion

        #region Genre/album/song tree view

        public IList<TreeViewNode> GenreTree
        {
            get;
            private set;
        }

        public TreeViewController GenreTreeController
        {
            get;
            private set;
        }

        public ISet<SongMetadataTreeViewNode> GenreTreeSelectedSongs
        {
            get
            {
                return GenreTreeController.Songs;
            }
        }

        public void ShowSongsInGenreTree(IEnumerable<SongMetadata> selectedSongs)
        {
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

            GenreTreeController.ClearMultiSelection();

            foreach (TreeViewNode rootNode in GenreTreeController.RootLevelNodes)
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
        }

        #endregion

        #region Directory tree view

        public IList<TreeViewNode> DirectoryTree
        {
            get;
            private set;
        }

        public TreeViewController DirectoryTreeController
        {
            get;
            private set;
        }

        public ISet<SongMetadataTreeViewNode> DirectoryTreeSelectedSongs
        {
            get
            {
                return DirectoryTreeController.Songs;
            }
        }

        public void ShowSongsInDirectoryTree(IEnumerable<SongMetadata> selectedSongs)
        {
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
        }

        // Expand/multiselect node if the path is found under it.
        private bool SearchAndSelectPath(TreeViewNode node, string path)
        {
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

            return false;
        }

        #endregion

        #region Utility

        private ISet<T> CollectSelectedElements<T>(IEnumerable<MusicCollectionItem> collection) where T : class
        {
            ISet<T> result = new SortedSet<T>();

            foreach (MusicCollectionItem item in collection)
            {
                if (item.IsSelected)
                {
                    result.Add(item.Content as T);
                }
            }

            return result;
        }

        #endregion
    }
}
