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

using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Auremo.Properties;

namespace Auremo
{
    public partial class MainWindow : Window
    {
        private SettingsWindow m_SettingsWindow = null;
        private TextWindow m_LicenseWindow = null;
        private AboutWindow m_AboutWindow = null;
        private DispatcherTimer m_Timer = null;
        private object m_DragSource = null;
        private IList<object> m_DragDropPayload = null;
        private string m_DragDropData = null;
        private Nullable<Point> m_DragStartPosition = null;
        private bool m_PropertyUpdateInProgress = false;
        private string m_AutoSearchString = "";
        private DateTime m_TimeOfLastAutoSearch = DateTime.MinValue;
        private object m_LastAutoSearchSender = null;
        private object m_LastLastAutoSearchHit = null;

        private const int m_AutoSearchMaxKeystrokeGap = 2500;

        private const string AddArtists = "add_artists";
        private const string AddGenres = "add_genres";
        private const string AddAlbums = "add_albums";
        private const string AddSongs = "add_songs";
        private const string AddStreams = "add_streams";
        private const string LoadPlaylist = "load_playlist";
        private const string AddPlaylistItems = "add_playlist_items";
        private const string MovePlaylistItems = "move_playlist_items";

        #region Start-up, construction and destruction

        public MainWindow()
        {
            InitializeComponent();
            InitializeComplexObjects();
            SetUpDataBindings();
            SetUpTreeViewControllers();
            CreateTimer(Settings.Default.ViewUpdateInterval);
            ApplyInitialSettings();
            SetInitialWindowState();
            Update();
        }

        public DataModel DataModel
        {
            get;
            private set;
        }

        private void InitializeComplexObjects()
        {
            DataModel = new DataModel(this);
        }

        private void SetUpDataBindings()
        {
            NameScope.SetNameScope(m_QuickSearchResultsViewContextMenu, NameScope.GetNameScope(this));
            NameScope.SetNameScope(m_AdvancedSearchResultsViewContextMenu, NameScope.GetNameScope(this));
            NameScope.SetNameScope(m_StreamsViewContextMenu, NameScope.GetNameScope(this));
            NameScope.SetNameScope(m_SavedPlaylistsViewContextMenu, NameScope.GetNameScope(this));
            NameScope.SetNameScope(m_PlaylistViewContextMenu, NameScope.GetNameScope(this));
            DataContext = DataModel;
            DataModel.ServerStatus.PropertyChanged += new PropertyChangedEventHandler(OnServerStatusPropertyChanged);
        }

        private void SetUpTreeViewControllers()
        {
            m_DirectoryTree.Tag = DataModel.DatabaseView.DirectoryTreeController;
            m_ArtistTree.Tag = DataModel.DatabaseView.ArtistTreeController;
            m_GenreTree.Tag = DataModel.DatabaseView.GenreTreeController;
        }

        private void CreateTimer(int interval)
        {
            m_Timer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher);
            m_Timer.Tick += new EventHandler(OnTimerTick);
            SetTimerInterval(interval);
            m_Timer.Start();
        }
        
        private void SetInitialWindowState()
        {
            int x = Settings.Default.WindowX;
            int y = Settings.Default.WindowY;
            int w = Settings.Default.WindowW;
            int h = Settings.Default.WindowH;

            w = w >= (int)MinWidth ? w : (int)MinWidth;
            h = h >= (int)MinHeight ? h : (int)MinHeight;

            if (x < SystemParameters.VirtualScreenWidth && x + w > 0 && y < SystemParameters.VirtualScreenHeight && y + h > 0)
            {
                Left = x;
                Top = y;
                Width = w;
                Height = h;
            }

            Show();

            if (!Settings.Default.InitialSetupDone)
            {
                BringUpSettingsWindow();
            }
        }

        private void SaveWindowState()
        {
            if (Left < SystemParameters.VirtualScreenWidth && Left + Width > 0 && Top < SystemParameters.VirtualScreenHeight && Top + Height > 0)
            {
                Settings.Default.WindowX = (int)Left;
                Settings.Default.WindowY = (int)Top;
                Settings.Default.WindowW = (int)Width;
                Settings.Default.WindowH = (int)Height;
                Settings.Default.Save();
            }
        }

        #endregion

        #region Updating logic and helpers

        private void SetTimerInterval(int interval)
        {
            m_Timer.Interval = new TimeSpan(0, 0, 0, 0, interval);
        }

        private void OnTimerTick(Object sender, EventArgs e)
        {
            DataModel.ServerSession.UpdateConnection();
            Update();
        }

        private void Update()
        {
            if (DataModel.ServerSession.IsConnected)
            {
                DataModel.ServerStatus.Update();
                DataModel.OutputCollection.Update();
            }
        }

        private void OnServerStatusPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            m_PropertyUpdateInProgress = true;

            if (e.PropertyName == "OK")
            {
                if (!DataModel.ServerStatus.OK)
                {
                    m_QuickSearchBox.Text = "";
                    m_AdvancedSearchBox.Text = "";
                }
            }
            else if (e.PropertyName == "PlayPosition")
            {
                OnPlayPositionChangedOnServer();
            }
            else if (e.PropertyName == "Volume")
            {
                OnVolumeChanged();
            }

            m_PropertyUpdateInProgress = false;
        }

        private void OnPlayPositionChangedOnServer()
        {
            if (!m_SeekBarIsBeingDragged)
            {
                m_SeekBar.Value = DataModel.ServerStatus.PlayPosition;
                m_PlayPosition.Content = Utils.IntToTimecode(DataModel.ServerStatus.PlayPosition);
            }
        }

        private void OnVolumeChanged()
        {
            m_VolumeControl.IsEnabled = DataModel.ServerStatus.Volume.HasValue && Settings.Default.EnableVolumeControl;
            m_VolumeControl.Value = DataModel.ServerStatus.Volume.HasValue ? DataModel.ServerStatus.Volume.Value : 0;
        }

        #endregion

        #region Selection notifications to data model

        private void OnSelectedArtistsChanged(object sender, SelectionChangedEventArgs e)
        {
           DataModel.DatabaseView.OnSelectedArtistsChanged();
        }

        private void OnSelectedAlbumsBySelectedArtistsChanged(object sender, SelectionChangedEventArgs e)
        {
            DataModel.DatabaseView.OnSelectedAlbumsBySelectedArtistsChanged();
        }

        private void OnSelectedSongsOnSelectedAlbumsBySelectedArtistsChanged(object sender, SelectionChangedEventArgs e)
        {
            DataModel.DatabaseView.OnSelectedSongsOnSelectedAlbumsBySelectedArtistsChanged();
        }

        private void OnSelectedGenresChanged(object sender, SelectionChangedEventArgs e)
        {
            DataModel.DatabaseView.OnSelectedGenresChanged();
        }

        private void OnSelectedAlbumsOfSelectedGenresChanged(object sender, SelectionChangedEventArgs e)
        {
            DataModel.DatabaseView.OnSelectedAlbumsOfSelectedGenresChanged();
        }

        private void OnSelectedSongsOnSelectedAlbumsOfSelectedGenresChanged(object sender, SelectionChangedEventArgs e)
        {
            DataModel.DatabaseView.OnSelectedSongsOnSelectedAlbumsOfSelectedGenresChanged();
        }

        private void OnSelectedSavedPlaylistChanged(object sender, SelectionChangedEventArgs e)
        {
            DataModel.SavedPlaylists.SelectedPlaylist = m_SavedPlaylistsView.SelectedItem as string;
        }

        private void OnSelectedItemsOnSelectedSavedPlaylistChanged(object sender, SelectionChangedEventArgs e)
        {
            DataModel.SavedPlaylists.SelectedItemsOnSelectedPlaylist = Utils.ToTypedList<MusicCollectionItem>(m_ItemsOnSelectedSavedPlaylistView.SelectedItems);
        }

        private void OnSelectedPlaylistItemsChanged(object sender, SelectionChangedEventArgs e)
        {
            DataModel.Playlist.OnSelectedItemsChanged(Utils.ToTypedList<PlaylistItem>(m_PlaylistView.SelectedItems));
        }

        #endregion

        #region Whole window operations

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Handled && !m_Overlay.Active)
            {
                if (e.Key == Key.MediaPreviousTrack)
                {
                    Back();
                    e.Handled = true;
                }
                else if (e.Key == Key.Space && !m_QuickSearchBox.IsKeyboardFocused && !m_AdvancedSearchBox.IsKeyboardFocused && !AutoSearchInProgrss)
                {
                    TogglePlayPause();
                    e.Handled = true;
                }
                else if (e.Key == Key.MediaPlayPause)
                {
                    TogglePlayPause();
                    e.Handled = true;
                }
                else if (e.Key == Key.MediaStop)
                {
                    Stop();
                    e.Handled = true;
                }
                else if (e.Key == Key.MediaNextTrack)
                {
                    Skip();
                    e.Handled = true;
                }
                else if (e.Key == Key.VolumeDown)
                {
                    VolumeDown();
                    e.Handled = true;
                }
                else if (e.Key == Key.VolumeUp)
                {
                    VolumeUp();
                    e.Handled = true;
                }
            }
        }

        private void OnExit(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DataModel.QuickSearch.Terminate();
            DataModel.ServerSession.OnlineMode = false;

            if (m_SettingsWindow != null)
            {
                m_SettingsWindow.Close();
                m_SettingsWindow = null;
            }

            if (m_LicenseWindow != null)
            {
                m_LicenseWindow.Close();
                m_LicenseWindow = null;
            }

            if (m_AboutWindow != null)
            {
                m_AboutWindow.Close();
                m_AboutWindow = null;
            }

            SaveWindowState();
        }

        #endregion

        #region Main menu

        private void OnEditSettingsClicked(object sender, RoutedEventArgs e)
        {
            BringUpSettingsWindow();
        }

        private void OnExitClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnConnectClicked(object sender, RoutedEventArgs e)
        {
            DataModel.ServerSession.OnlineMode = true;
        }

        private void OnDisconnectClicked(object sender, RoutedEventArgs e)
        {
            DataModel.ServerSession.OnlineMode = false;
        }

        private void OnResetConnectionClicked(object sender, RoutedEventArgs e)
        {
            DataModel.ServerSession.OnlineMode = true;
            DataModel.ServerSession.Disconnect();
        }

        private void Reconnect()
        {
            DataModel.ServerSession.Disconnect();
            DataModel.ServerSession.OnlineMode = true;
        }

        private void OnEnableDisbaleOutput(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            Output output = checkBox.DataContext as Output;

            if (output.IsEnabled)
            {
                DataModel.ServerSession.DisableOutput(output.Index);
            }
            else
            {
                DataModel.ServerSession.EnableOutput(output.Index);
            }

            Update();
        }

        private void OnToggleSingleMode(object sender, RoutedEventArgs e)
        {
            DataModel.ServerSession.Single(!DataModel.ServerStatus.IsOnSingle);
            Update();
        }

        private void OnToggleConsumeMode(object sender, RoutedEventArgs e)
        {
            DataModel.ServerSession.Consume(!DataModel.ServerStatus.IsOnConsume);
            Update();
        }

        private void OnViewLicenseClicked(object sender, RoutedEventArgs e)
        {
            BringUpLicenseWindow();
        }

        private void OnAboutClicked(object sender, RoutedEventArgs e)
        {
            BringUpAboutWindow();
        }

        private void OnNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        #endregion

        #region Music collection and playlist

        #region Key, mouse and menu events common to multiple controls

        private void OnAlbumsOfSelectedGenresViewKeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Handled)
            {
                if (e.Key == Key.Enter)
                {
                    DataGrid grid = sender as DataGrid;
                    SendItemsToPlaylist(sender, Utils.GetSortedSelection(m_SongsOnSelectedGenreAlbumsView));
                    e.Handled = true;
                }
            }
        }

        private void OnMusicCollectionDataGridKeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Handled)
            {
                if (e.Key == Key.Enter)
                {
                    DataGrid grid = sender as DataGrid;

                    if (grid == m_AlbumsOfSelectedGenresView)
                    {
                        // Filter album contents by selected genres.
                        // TODO: maybe add genre-filtered albums into the database to get
                        // out of kludges like this?
                        SendItemsToPlaylist(sender, Utils.ToTypedList<MusicCollectionItem>(m_SongsOnSelectedGenreAlbumsView.Items).OrderBy(el => el.Position));
                    }
                    else
                    {
                        SendItemsToPlaylist(sender, Utils.GetSortedSelection(grid));
                    }
                    
                    e.Handled = true;
                }
                else if (sender == m_StreamsView)
                {
                    OnStreamsViewKeyDown(sender, e);
                }
            }
        }

        private void SendItemsToPlaylist(object sourceControl, IEnumerable<object> items)
        {
            bool stringsAreArtists = sourceControl == m_ArtistsView;

            if (Settings.Default.SendToPlaylistMethod == SendToPlaylistMethod.AddAsNext.ToString())
            {
                AddObjectsToPlaylist(items, stringsAreArtists, DataModel.ServerStatus.CurrentSongIndex + 1);
            }
            else if (Settings.Default.SendToPlaylistMethod == SendToPlaylistMethod.ReplaceAndPlay.ToString())
            {
                DataModel.ServerSession.Clear();
                AddObjectsToPlaylist(items, stringsAreArtists, 0);
                DataModel.ServerSession.Play();
            }
            else // Assume SendToPlaylistMethod.Append as the default
            {
                AddObjectsToPlaylist(items, stringsAreArtists);
            }

            Update();
        }

        private void OnCollectionTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!e.Handled && e.Text != null && e.Text.Length == 1)
            {
                CollectionAutoSearch(sender, e.Text[0]);
            }
        }

        private void OnAlbumsOfSelectedGenresViewDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            // Filter album contents by selected genres.
            if (!e.Handled)
            {
                DataGridRow row = DataGridRowBeingClicked(m_AlbumsOfSelectedGenresView, e);

                if (row != null)
                {
                    // TODO: this is a kind of kludge as well.
                    SendItemsToPlaylist(sender, Utils.ToTypedList<SongMetadata>(m_SongsOnSelectedGenreAlbumsView.Items));
                    e.Handled = true;
                }
            }
        }

        private void OnMusicCollectionDataGridDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            if (!e.Handled)
            {
                DataGrid grid = sender as DataGrid;
                DataGridRow row = DataGridRowBeingClicked(grid, e);

                if (row != null)
                {
                    if (grid == m_AlbumsOfSelectedGenresView)
                    {
                        // Filter album contents by selected genres.
                        // TODO: another kludge.
                        SendItemsToPlaylist(sender, Utils.ToTypedList<object>(m_SongsOnSelectedGenreAlbumsView.Items));
                    }
                    else
                    {
                        IList<object> item = new List<object>();
                        item.Add(row.Item);
                        SendItemsToPlaylist(sender, item);
                    }
                    
                    e.Handled = true;
                }
            }
        }

        public void OnAddToPlaylistAsLastClicked(object sender, RoutedEventArgs e)
        {
            AddToPlaylistContextMenuViaContextMenu(sender, DataModel.Playlist.Items.Count);
        }

        public void OnAddToPlaylistAsNextClicked(object sender, RoutedEventArgs e)
        {
            AddToPlaylistContextMenuViaContextMenu(sender, DataModel.ServerStatus.CurrentSongIndex + 1);
        }

        public void OnPlayThisClicked(object sender, RoutedEventArgs e)
        {
            DataModel.ServerSession.Clear();
            AddToPlaylistContextMenuViaContextMenu(sender, 0);
            DataModel.ServerSession.Play();
        }

        public void AddToPlaylistContextMenuViaContextMenu(object sender, int insertPosition)
        {
            MenuItem menuItem = sender as MenuItem;
            ContextMenu menu = menuItem.Parent as ContextMenu;
            UIElement senderView = menu.PlacementTarget;
            int position = insertPosition;

            if (senderView == m_AlbumsOfSelectedGenresView)
            {
                // Filter album contents by selected genres.
                AddObjectsToPlaylist(Utils.ToTypedList<object>(m_SongsOnSelectedGenreAlbumsView.Items), false, position);
            }
            else if (senderView is DataGrid)
            {
                DataGrid list = senderView as DataGrid;
                bool stringsAreArtists = list == m_ArtistsView;
                AddObjectsToPlaylist(Utils.ToTypedList<object>(list.SelectedItems), stringsAreArtists, position);
            }
            else if (senderView is TreeView)
            {
                TreeViewController controller = TreeViewControllerOf(senderView as TreeView);
                ISet<SongMetadataTreeViewNode> selection = controller.Songs;

                if (selection != null)
                {
                    foreach (SongMetadataTreeViewNode node in selection)
                    {
                        position = AddPlayableToPlaylist(node.Song, position);
                    }
                }
            }

            Update();
        }

        public void OnRescanMusicCollectionClicked(object sender, RoutedEventArgs e)
        {
            DataModel.ServerSession.Update();
        }

        public void OnRescanPlaylistsCollectionClicked(object sender, RoutedEventArgs e)
        {
            DataModel.SavedPlaylists.Refresh();
        }

        public void OnServerClicked(object sender, RoutedEventArgs e)
        {
            DependencyObject dep = sender as DependencyObject;

            while (dep != null && !(dep is MenuItem))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            MenuItem menuItem = dep as MenuItem;
            Server server = menuItem.Header as Server;
            DataModel.Servers.SelectedServerIndex = server.ItemIndex;
            Settings.Default.SelectedServerIndex = server.ItemIndex;
            Settings.Default.Save();
        }

        private IList<SongMetadata> SelectedLocalSongsOnPlaylist()
        {
            IList<SongMetadata> result = new List<SongMetadata>();

            foreach (PlaylistItem selectedItem in m_PlaylistView.SelectedItems)
            {
                if (selectedItem.Content is SongMetadata)
                {
                    SongMetadata song = selectedItem.Content as SongMetadata;

                    if (song.IsLocal)
                    {
                        result.Add(selectedItem.Content as SongMetadata);
                    }
                }
            }

            return result;
        }

        #endregion

        #region List selection and drag & drop

        private void OnDataGridRowMouseDown(object sender, MouseButtonEventArgs e)
        {
            DataGrid dataGrid = sender as DataGrid;
            dataGrid.Focus();

            if (e.ClickCount == 1)
            {
                DataGridRow row = DataGridRowBeingClicked(dataGrid, e);

                if (row == null)
                {
                    if (ViewBackgroundWasClicked(dataGrid, e) && Keyboard.Modifiers == ModifierKeys.None)
                    {
                        dataGrid.UnselectAll();
                        dataGrid.CurrentItem = null;
                        e.Handled = true;
                    }
                }
                else
                {
                    if (dataGrid.SelectionMode == DataGridSelectionMode.Single)
                    {
                        row.IsSelected = true;
                        e.Handled = true;
                    }
                    else if (Keyboard.Modifiers == ModifierKeys.None)
                    {
                        if (row.IsSelected)
                        {
                            e.Handled = true;
                        }
                        else
                        {
                            dataGrid.UnselectAll();
                            row.IsSelected = true;
                        }

                        dataGrid.CurrentItem = row.Item;
                    }
                    else if (Keyboard.Modifiers == ModifierKeys.Shift)
                    {
                        if (dataGrid.CurrentItem == null)
                        {
                            dataGrid.UnselectAll();
                            row.IsSelected = true;
                            dataGrid.CurrentItem = row.Item;
                        }
                        else
                        {
                            int startIndex = (dataGrid.CurrentItem as DataGridItem).Position;
                            int endIndex = (row.Item as DataGridItem).Position;
                            int minIndex = Math.Min(startIndex, endIndex);
                            int maxIndex = Math.Max(startIndex, endIndex);

                            foreach (object o in dataGrid.Items)
                            {
                                DataGridItem item = o as DataGridItem;
                                item.IsSelected = item.Position >= minIndex && item.Position <= maxIndex;
                            }
                        }
                    }
                    else if (Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        dataGrid.CurrentItem = row.Item;
                        row.IsSelected = !row.IsSelected;
                        e.Handled = true;
                    }

                    if (row.IsSelected)
                    {
                        m_DragSource = dataGrid;
                        m_DragStartPosition = e.GetPosition(null);
                    }
                }
            }
        }

        private void OnDataGridRowMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                DataGrid dataGrid = sender as DataGrid;

                if (dataGrid.SelectionMode == DataGridSelectionMode.Extended)
                {
                    DataGridRow row = DataGridRowBeingClicked(dataGrid, e);

                    if (row != null && Keyboard.Modifiers == ModifierKeys.None)
                    {
                        dataGrid.UnselectAll();
                        dataGrid.SelectedItem = dataGrid.CurrentItem;
                    }
                }

                m_DragSource = null;
                m_DragStartPosition = null;
            }
        }

        private void OnMouseMoveDragDrop(object sender, MouseEventArgs e)
        {
            if (m_DragStartPosition.HasValue && m_DragSource != null)
            {
                Vector dragDistance = e.GetPosition(null) - m_DragStartPosition.Value;

                if (e.LeftButton == MouseButtonState.Pressed &&
                    (Math.Abs(dragDistance.X) > SystemParameters.MinimumHorizontalDragDistance ||
                     Math.Abs(dragDistance.Y) > SystemParameters.MinimumVerticalDragDistance))
                {
                    // Using an internal m_DragDropPayload is cheating, but the
                    // the standard (through clipboard?) system is pure evil and
                    // since we don't need drag & drop across application borders,
                    // this will do for now.
                    //IList<object> payload = new List<object>();
                    IList<object> payload = null;

                    if (m_DragSource == m_SavedPlaylistsView)
                    {
                        if (m_SavedPlaylistsView.SelectedItem != null)
                        {
                            payload = new List<object>();
                            payload.Add(m_SavedPlaylistsView.SelectedItem);
                        }
                    }
                    else if (m_DragSource == m_PlaylistView)
                    {
                        payload = Utils.GetPlaylistSortedSelection(m_PlaylistView);
                    }
                    else if (m_DragSource is DataGrid)
                    {
                        payload = Utils.GetSortedSelection(m_DragSource as DataGrid);
                    }
                    else if (m_DragSource is TreeView)
                    {
                        TreeViewController controller = TreeViewControllerOf(m_DragSource as TreeView);
                        payload = controller.Songs == null ? new List<object>() : controller.Songs.Select(el => el.Song).ToList<object>();
                    }

                    if (payload != null && payload.Count > 0)
                    {
                        DragDropEffects mode = sender == m_PlaylistView ? DragDropEffects.Move : DragDropEffects.Copy;
                        m_DragDropPayload = payload;
                        m_DragDropData = GetDragDropDataString(m_DragSource);
                        m_MousePointerHint.Content = DragDropPayloadDescription();
                        DragDrop.DoDragDrop((DependencyObject)sender, m_DragDropData, mode);
                    }

                    m_DragStartPosition = null;
                }
            }
        }

        private string DragDropPayloadDescription()
        {
            const int maxLines = 4;
            int nameLines = m_DragDropPayload.Count <= maxLines ? m_DragDropPayload.Count : maxLines - 1;

            StringBuilder result = new StringBuilder();

            for (int i = 0; i < nameLines; ++i)
            {
                if (i > 0)
                {
                    result.Append("\n");
                }

                object item = m_DragDropPayload[i];

                if (item is string)
                {
                    result.Append(item as string);
                }
                else if (item is AlbumMetadata)
                {
                    result.Append((item as AlbumMetadata).Title);
                }
                else if (item is Playable)
                {
                    result.Append((item as Playable).DisplayName);
                }
                else if (item is PlaylistItem)
                {
                    result.Append((item as PlaylistItem).Playable.DisplayName);
                }
            }

            if (m_DragDropPayload.Count > nameLines)
            {
                result.Append("\n+" + (m_DragDropPayload.Count - nameLines) + " more...");
            }

            return result.ToString();
        }

        private void OnPlaylistViewDragOver(object sender, DragEventArgs e)
        {
            if (m_DragDropPayload != null)
            {
                m_MousePointerHint.IsOpen = true;
                m_MousePointerHint.Visibility = Visibility.Visible;

                if (!m_PlaylistView.Items.IsEmpty)
                {
                    if (m_DragDropData != LoadPlaylist)
                    {
                        Point mousePosition = e.GetPosition(m_PlaylistView);

                        m_MousePointerHint.Placement = PlacementMode.Relative;
                        m_MousePointerHint.PlacementTarget = m_PlaylistView;
                        m_MousePointerHint.HorizontalOffset = mousePosition.X + 10;
                        m_MousePointerHint.VerticalOffset = mousePosition.Y - 6;

                        int targetRow = DropTargetRowIndex(e);
                        DataGridRow row = null;

                        if (targetRow >= 0)
                        {
                            row = m_PlaylistView.ItemContainerGenerator.ContainerFromIndex(targetRow) as DataGridRow;
                        }

                        if (row == null)
                        {
                            DataGridRow lastItem = m_PlaylistView.ItemContainerGenerator.ContainerFromIndex(m_PlaylistView.Items.Count - 1) as DataGridRow;
                            Rect bounds = VisualTreeHelper.GetDescendantBounds(lastItem);
                            GeneralTransform transform = lastItem.TransformToAncestor(m_PlaylistView);
                            Point bottomOfItem = transform.Transform(bounds.BottomLeft);
                            m_DropPositionIndicator.Y1 = bottomOfItem.Y;
                        }
                        else
                        {
                            Rect bounds = VisualTreeHelper.GetDescendantBounds(row);
                            GeneralTransform transform = row.TransformToAncestor(m_PlaylistView);
                            Point topOfItem = transform.Transform(bounds.TopLeft);
                            m_DropPositionIndicator.Y1 = topOfItem.Y;
                        }

                        m_DropPositionIndicator.X1 = 0;
                        m_DropPositionIndicator.X2 = m_PlaylistView.ActualWidth;
                        m_DropPositionIndicator.Y2 = m_DropPositionIndicator.Y1;
                        m_DropPositionIndicator.Visibility = Visibility.Visible;
                    }
                }
            }
            else
            {
                m_MousePointerHint.IsOpen = false;
                m_MousePointerHint.Visibility = Visibility.Hidden;
                m_DropPositionIndicator.Visibility = Visibility.Hidden;
            }
        }

        private void OnPlaylistViewDragLeave(object sender, DragEventArgs e)
        {
            m_MousePointerHint.IsOpen = false;
            m_MousePointerHint.Visibility = Visibility.Hidden;
            m_DropPositionIndicator.Visibility = Visibility.Hidden;
        }

        private void OnPlaylistViewDrop(object sender, DragEventArgs e)
        {
            if (m_DragDropPayload != null)
            {
                int targetRow = DropTargetRowIndex(e);
                string data = (string)e.Data.GetData(typeof(string));

                if (data == LoadPlaylist)
                {
                    LoadSavedPlaylist(m_DragDropPayload[0] as string);
                }
                else if (data == MovePlaylistItems)
                {
                    foreach (object o in m_DragDropPayload)
                    {
                        PlaylistItem item = (PlaylistItem)o;

                        if (item.Position < targetRow)
                        {
                            DataModel.ServerSession.MoveId(item.Id, targetRow - 1);
                        }
                        else
                        {
                            DataModel.ServerSession.MoveId(item.Id, targetRow++);
                        }
                    }
                }
                else if (data == AddGenres)
                {
                    foreach (object o in m_DragDropPayload)
                    {
                        targetRow = AddObjectToPlaylist(o, false, targetRow);
                    }
                }
                else if (data == AddArtists || data == AddAlbums || data == AddSongs || data == AddStreams || data == AddPlaylistItems)
                {
                    foreach (object o in m_DragDropPayload)
                    {
                        targetRow = AddObjectToPlaylist(o, true, targetRow);
                    }
                }

                m_DragDropPayload = null;
                m_DragDropData = null;
                Update();
            }

            m_MousePointerHint.IsOpen = false;
            m_MousePointerHint.Visibility = Visibility.Hidden;
            m_DropPositionIndicator.Visibility = Visibility.Hidden;
        }

        #endregion

        #region TreeView handling (browsing, selection, drag & drop)

        private void OnTreeViewMouseDown(object sender, MouseButtonEventArgs e)
        {
            TreeView tree = sender as TreeView;
            TreeViewItem item = TreeViewItemBeingClicked(tree, e);
            tree.Focus();

            if (item == null)
            {
                if (ViewBackgroundWasClicked(tree, e))
                {
                    (tree.Tag as TreeViewController).ClearMultiSelection();
                    e.Handled = true;
                }
            }
            else if (item.Header is TreeViewNode)
            {
                TreeViewNode node = item.Header as TreeViewNode;

                if (Keyboard.Modifiers == ModifierKeys.None)
                {
                    node.Controller.Current = node;
                    node.Controller.Pivot = node;

                    if (!node.IsMultiSelected)
                    {
                        node.Controller.ClearMultiSelection();
                        node.IsMultiSelected = true;
                    }
                    else if (e.ClickCount == 1)
                    {
                        m_DragSource = sender;
                        m_DragStartPosition = e.GetPosition(null);
                    }
                    else
                    {
                        IList<object> items = new List<object>();

                        foreach (SongMetadataTreeViewNode leaf in node.Controller.Songs)
                        {
                            items.Add(leaf.Song);
                        }

                        // TODO: is all this really necessary?
                        SendItemsToPlaylist(sender, items);
                    }
                }
                else if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    node.Controller.Current = node;
                    node.IsMultiSelected = !node.IsMultiSelected;
                    node.Controller.Pivot = node.IsMultiSelected ? node : null;
                }
                else if (Keyboard.Modifiers == ModifierKeys.Shift)
                {
                    node.Controller.Current = node;
                    node.Controller.ClearMultiSelection();

                    if (node.Controller.Pivot == null)
                    {
                        node.IsMultiSelected = true;
                        node.Controller.Pivot = node;
                    }
                    else
                    {
                        node.Controller.SelectRange(node);
                    }
                }

                if (e.ClickCount == 1 && node.IsMultiSelected)
                {
                    m_DragSource = sender;
                    m_DragStartPosition = e.GetPosition(null);
                }

                e.Handled = true;
            }
        }

        private void OnTreeViewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && Keyboard.Modifiers == ModifierKeys.None)
            {
                TreeViewItem item = TreeViewItemBeingClicked(sender as TreeView, e);

                if (item != null && item.Header is TreeViewNode)
                {
                    TreeViewNode node = item.Header as TreeViewNode;
                    node.Controller.ClearMultiSelection();
                    node.IsMultiSelected = true;
                    node.Controller.Pivot = node;
                }
            }
        }

        private void OnTreeViewKeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Handled)
            {
                TreeView tree = sender as TreeView;
                TreeViewController controller = tree.Tag as TreeViewController;

                if (Keyboard.Modifiers == ModifierKeys.None || Keyboard.Modifiers == ModifierKeys.Shift)
                {
                    bool currentChanged = false;

                    if (e.Key == Key.Up && EnsureTreeViewHasCurrentNode(controller))
                    {
                        controller.Current = controller.Previous;
                        currentChanged = true;
                        e.Handled = true;
                    }
                    else if (e.Key == Key.Down && EnsureTreeViewHasCurrentNode(controller))
                    {
                        controller.Current = controller.Next;
                        currentChanged = true;
                        e.Handled = true;
                    }
                    else if (e.Key == Key.Left && EnsureTreeViewHasCurrentNode(controller))
                    {
                        if (controller.Current.Parent != null)
                        {
                            controller.Current = controller.Current.Parent;
                            currentChanged = true;
                        }

                        e.Handled = true;
                    }
                    else if (e.Key == Key.Right && EnsureTreeViewHasCurrentNode(controller))
                    {
                        if (controller.Current.Children.Count > 0)
                        {
                            controller.Current.IsExpanded = true;
                            controller.Current = controller.Current.Children[0];
                            currentChanged = true;
                            e.Handled = true;
                        }
                    }
                    else if (e.Key == Key.Enter)
                    {
                        IList<object> items = new List<object>();

                        foreach (SongMetadataTreeViewNode leaf in controller.Songs)
                        {
                            items.Add(leaf.Song);
                        }

                        // TODO: is all this really necessary?
                        SendItemsToPlaylist(sender, items);
                        e.Handled = true;
                    }

                    if (currentChanged)
                    {
                        TreeViewItem item = GetTreeViewItem(tree, controller.Current);

                        if (item != null)
                        {
                            item.BringIntoView();
                        }

                        if (Keyboard.Modifiers == ModifierKeys.None)
                        {
                            controller.ClearMultiSelection();
                            controller.Current.IsMultiSelected = true;
                            controller.Pivot = controller.Current;
                        }
                        else if (Keyboard.Modifiers == ModifierKeys.Shift)
                        {
                            if (controller.Pivot == null)
                            {
                                controller.ClearMultiSelection();
                                controller.Current.IsMultiSelected = true;
                                controller.Pivot = controller.Current;
                            }
                            else
                            {
                                controller.ClearMultiSelection();
                                controller.SelectRange(controller.Current);
                            }
                        }
                    }
                }
            }
        }

        private void OnTreeViewSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // Cancel the selection. Use the controller multiselection system instead.
            TreeViewNode node = e.NewValue as TreeViewNode;

            if (node != null)
            {
                node.IsSelected = false;
            }
        }

        #endregion

        #region Specialized events for individual controls

        private void OnAdvancedSearchClicked(object sender, RoutedEventArgs e)
        {
            DataModel.AdvancedSearch.Search();
        }

        #region Streams collection

        private void OnStreamsViewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F2 && m_StreamsView.SelectedItems.Count == 1)
            {
                StartRenameStreamQuery();
                e.Handled = true;
            }
            else if (e.Key == Key.Delete)
            {
                DeleteSelectedStreams();
                e.Handled = true;
            }
        }

        private void OnRenameSelectedStreamClicked(object sender, RoutedEventArgs e)
        {
            StartRenameStreamQuery();
        }

        private void OnDeleteSelectedStreamsClicked(object sender, RoutedEventArgs e)
        {
            DeleteSelectedStreams();
        }

        private void OnAddStreamURLClicked(object sender, RoutedEventArgs e)
        {
            StartAddNewStreamQuery();
        }

        private void OnAddStreamsFromFileClicked(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "Add stream files";
            dialog.Multiselect = true;
            dialog.Filter = "Playlist Files|*.pls;*.m3u";

            bool? dialogResult = dialog.ShowDialog();

            if (dialogResult.HasValue && dialogResult.Value)
            {
                PLSParser plsParser = new PLSParser();
                M3UParser m3uParser = new M3UParser();
                List<StreamMetadata> streamsToAdd = new List<StreamMetadata>();

                foreach (string filename in dialog.FileNames)
                {
                    IEnumerable<StreamMetadata> streams = null;

                    if (filename.ToLowerInvariant().EndsWith(".pls"))
                    {
                        streams = plsParser.ParseFile(filename);
                    }
                    else if (filename.ToLowerInvariant().EndsWith(".m3u"))
                    {
                        streams = m3uParser.ParseFile(filename);
                    }
                    
                    if (streams != null)
                    {
                        streamsToAdd.AddRange(streams);
                    }
                }

                DataModel.StreamsCollection.Add(streamsToAdd);
            }
        }

        private void OnSaveSelectedStreamsToFileClicked(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Title = "Save streams";
            dialog.Filter = "Playlist Files|*.pls";

            bool? dialogResult = dialog.ShowDialog();

            if (dialogResult.HasValue && dialogResult.Value)
            {
                string filename = dialog.FileName;
                string playlist = PlaylistWriter.Write(Utils.ToContentList<StreamMetadata>(m_StreamsView.SelectedItems));

                if (playlist != null)
                {
                    File.WriteAllText(filename, playlist);
                }
            }
        }

        private void DeleteSelectedStreams()
        {
            DataModel.StreamsCollection.Delete(Utils.ToTypedList<MusicCollectionItem>(m_StreamsView.SelectedItems));
        }

        #endregion

        #region Saved playlists collection

        private void OnSavedPlaylistsViewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoadSelectedSavedPlaylist();
                e.Handled = true;
            }
            else if (e.Key == Key.F2)
            {
                RenameSavedPlaylist();
                e.Handled = true;
            }
            else if (e.Key == Key.Delete)
            {
                DeleteSelectedSavedPlaylist();
                e.Handled = true;
            }
        }

        private void OnSavedPlaylistsViewDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.None)
            {
                LoadSelectedSavedPlaylist();
            }
        }

        private void OnSendSavedPlaylistToPlaylistClicked(object sender, RoutedEventArgs e)
        {
            LoadSelectedSavedPlaylist();
        }

        private void LoadSelectedSavedPlaylist()
        {
            object selectedPlaylist = m_SavedPlaylistsView.SelectedItem;

            if (selectedPlaylist != null)
            {
                LoadSavedPlaylist(selectedPlaylist as string);
            }
        }

        private void LoadSavedPlaylist(string name)
        {
            DataModel.ServerSession.Clear();
            DataModel.ServerSession.Load(name);
            DataModel.SavedPlaylists.CurrentPlaylistName = name;
            Update();
        }

        private void OnRenameSavedPlaylistClicked(object sender, RoutedEventArgs e)
        {
            RenameSavedPlaylist();
        }

        private void RenameSavedPlaylist()
        {
            object selectedPlaylist = m_SavedPlaylistsView.SelectedItem;

            if (selectedPlaylist != null)
            {
                StartRenameSavedPlaylistQuery(selectedPlaylist as string);
            }
        }


        private void OnDeleteSavedPlaylistClicked(object sender, RoutedEventArgs e)
        {
            DeleteSelectedSavedPlaylist();
        }

        private void DeleteSelectedSavedPlaylist()
        {
            object selectedPlaylist = m_SavedPlaylistsView.SelectedItem;

            if (selectedPlaylist != null)
            {
                DataModel.ServerSession.Rm(selectedPlaylist as string);
                DataModel.SavedPlaylists.Refresh();
            }
        }

        #endregion

        #endregion

        #region Playlist

        private void OnPlaylistViewKeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Handled)
            {
                if (e.Key == Key.Enter)
                {
                    if (m_PlaylistView.SelectedItems.Count == 1)
                    {
                        object o = m_PlaylistView.SelectedItems[0];

                        if (o is PlaylistItem)
                        {
                            PlaylistItem item = o as PlaylistItem;
                            DataModel.ServerSession.PlayId(item.Id);
                            Update();
                        }

                        e.Handled = true;
                    }
                }
                else if (e.Key == Key.Delete)
                {
                    DeleteSelectedItemsFromPlaylist();
                    e.Handled = true;
                }
            }
        }

        private void OnPlaylistViewDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = DataGridRowBeingClicked(m_PlaylistView, e);

            if (row != null)
            {
                PlaylistItem item = row.Item as PlaylistItem;
                DataModel.ServerSession.PlayId(item.Id);
                Update();
            }
        }

        private void OnClearPlaylistViewClicked(object sender, RoutedEventArgs e)
        {
            DataModel.ServerSession.Clear();
            DataModel.SavedPlaylists.CurrentPlaylistName = "";
            Update();
        }

        private void OnRemoveSelectedPlaylistItemsClicked(object sender, RoutedEventArgs e)
        {
            DeleteSelectedItemsFromPlaylist();
        }

        private void OnCropToSelectedPlaylistItemsClicked(object sender, RoutedEventArgs e)
        {
            if (m_PlaylistView.SelectedItems.Count > 0)
            {
                ISet<int> keepItems = new SortedSet<int>();

                foreach (Object o in m_PlaylistView.SelectedItems)
                {
                    PlaylistItem item = o as PlaylistItem;
                    keepItems.Add(item.Id);
                }

                foreach (PlaylistItem item in DataModel.Playlist.Items)
                {
                    if (!keepItems.Contains(item.Id))
                    {
                        DataModel.ServerSession.DeleteId(item.Id);
                    }
                }

                Update();
            }
        }

        private void OnSavePlaylistAsClicked(object sender, RoutedEventArgs e)
        {
            StartAddNewPlaylistAsQuery(DataModel.SavedPlaylists.CurrentPlaylistName);
        }
       
        private void OnDedupPlaylistViewClicked(object sender, RoutedEventArgs e)
        {
            ISet<string> songPathsOnPlaylist = new SortedSet<string>();
            IList<int> playlistIDsOfDuplicates = new List<int>();

            foreach (PlaylistItem item in DataModel.Playlist.Items)
            {
                if (!songPathsOnPlaylist.Add(item.Playable.Path))
                {
                    playlistIDsOfDuplicates.Add(item.Id);
                }
            }

            foreach (int id in playlistIDsOfDuplicates)
            {
                DataModel.ServerSession.DeleteId(id);
            }
        }

        private void OnShufflePlaylistClicked(object sender, RoutedEventArgs e)
        {
            DataModel.ServerSession.Shuffle();
        }

        private void OnShowInArtistsListClicked(object sender, RoutedEventArgs e)
        {
            IList<SongMetadata> selection = SelectedLocalSongsOnPlaylist();

            if (selection.Count > 0)
            {
                DataModel.DatabaseView.ShowSongsInArtistList(selection);
                m_ArtistListTab.IsSelected = true;
            }
        }

        private void OnShowInArtistsTreeClicked(object sender, RoutedEventArgs e)
        {
            IList<SongMetadata> selection = SelectedLocalSongsOnPlaylist();

            if (selection.Count > 0)
            {
                DataModel.DatabaseView.ShowSongsInArtistTree(selection);
                m_ArtistTreeTab.IsSelected = true;
            }
        }

        private void OnShowInGenreListClicked(object sender, RoutedEventArgs e)
        {
            IList<SongMetadata> selection = SelectedLocalSongsOnPlaylist();

            if (selection.Count > 0)
            {
                DataModel.DatabaseView.ShowSongsInGenreList(selection);
                m_GenreListTab.IsSelected = true;
            }
        }

        private void OnShowInGenreTreeClicked(object sender, RoutedEventArgs e)
        {
            IList<SongMetadata> selection = SelectedLocalSongsOnPlaylist();

            if (selection.Count > 0)
            {
                DataModel.DatabaseView.ShowSongsInGenreTree(selection);
                m_GenreTreeTab.IsSelected = true;
            }
        }

        private void OnShowInFilesystemTreeClicked(object sender, RoutedEventArgs e)
        {
            IList<SongMetadata> selection = SelectedLocalSongsOnPlaylist();

            if (selection.Count > 0)
            {
                DataModel.DatabaseView.ShowSongsInDirectoryTree(selection);
                m_FilesystemTab.IsSelected = true;
            }
        }

        private void DeleteSelectedItemsFromPlaylist()
        {
            foreach (object o in m_PlaylistView.SelectedItems)
            {
                if (o is PlaylistItem)
                {
                    PlaylistItem item = o as PlaylistItem;
                    DataModel.ServerSession.DeleteId(item.Id);
                }
            }

            Update();
        }

        #endregion

        #region Autosearch

        private bool AutoSearchInProgrss
        {
            get
            {
                return m_AutoSearchString.Length > 0 && DateTime.Now.Subtract(m_TimeOfLastAutoSearch).TotalMilliseconds <= m_AutoSearchMaxKeystrokeGap;
            }
        }

        private bool CollectionAutoSearch(object sender, char c)
        {
            bool selectionUnchanged = false;

            if (sender is DataGrid)
            {
                DataGrid grid = sender as DataGrid;
                selectionUnchanged = grid.SelectedItems.Count == 1 && grid.SelectedItem == m_LastLastAutoSearchHit;
            }
            else if (sender is TreeView)
            {
                TreeViewController controller = (sender as TreeView).Tag as TreeViewController;
                selectionUnchanged = controller.MultiSelection.Count == 1 && controller.MultiSelection[0] == m_LastLastAutoSearchHit;
            }

            if (sender != m_LastAutoSearchSender || !selectionUnchanged || DateTime.Now.Subtract(m_TimeOfLastAutoSearch).TotalMilliseconds > m_AutoSearchMaxKeystrokeGap)
            {
                m_AutoSearchString = "";
            }

            m_TimeOfLastAutoSearch = DateTime.Now;
            m_LastAutoSearchSender = sender;
            bool searchAgain = false;

            if (c == '\b')
            {
                if (m_AutoSearchString.Length > 0)
                {
                    m_AutoSearchString = m_AutoSearchString.Remove(m_AutoSearchString.Length - 1);
                    searchAgain = m_AutoSearchString.Length > 0;
                }
            }
            else if (!char.IsControl(c) && !char.IsSurrogate(c))
            {
                m_AutoSearchString = (m_AutoSearchString + c).ToLowerInvariant();
                searchAgain = true;
            }
            else
            {
                m_TimeOfLastAutoSearch = DateTime.MinValue;
            }

            if (searchAgain)
            {
                if (sender is DataGrid)
                {
                    DataGrid grid = sender as DataGrid;

                    foreach (DataGridItem item in Utils.ToTypedList<DataGridItem>(grid.Items))
                    {
                        object o = item.Content;

                        if (o is string && (o as string).ToLowerInvariant().StartsWith(m_AutoSearchString) ||
                           o is AlbumMetadata && (o as AlbumMetadata).Title.ToLowerInvariant().StartsWith(m_AutoSearchString) ||
                           o is Playable && (o as Playable).Title != null && (o as Playable).Title.ToLowerInvariant().StartsWith(m_AutoSearchString))
                        {
                            grid.CurrentItem = item;
                            grid.SelectedItem = item;
                            grid.ScrollIntoView(item);
                            m_LastLastAutoSearchHit = item;

                            return true;
                        }
                    }
                }
                else if (sender is TreeView)
                {
                    TreeView tree = sender as TreeView;
                    TreeViewNode item = CollectionAutoSearchTreeViewRecursively(Utils.ToTypedList<TreeViewNode>(tree.Items));

                    if (item != null)
                    {
                        item.Controller.ClearMultiSelection();
                        item.IsMultiSelected = true;
                        item.IsSelected = true;
                        item.Controller.Current = item;
                        item.Controller.Pivot = item;
                        m_LastLastAutoSearchHit = item;

                        return true;
                    }
                }
            }

            return false;
        }

        private TreeViewNode CollectionAutoSearchTreeViewRecursively(IEnumerable<TreeViewNode> nodes)
        {
            foreach (TreeViewNode node in nodes)
            {
                if (node.DisplayString.ToLowerInvariant().StartsWith(m_AutoSearchString))
                {
                    return node;
                }
                else if (node.IsExpanded)
                {
                    TreeViewNode result = CollectionAutoSearchTreeViewRecursively(Utils.ToTypedList<TreeViewNode>(node.Children));

                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            return null;
        }

        #endregion

        #region Helpers for adding items to the playlist

        private void AddObjectsToPlaylist(IEnumerable<object> items, bool stringsAreArtists)
        {
            foreach (object item in items)
            {
                AddObjectToPlaylist(item, stringsAreArtists);
            }
        }

        private void AddObjectsToPlaylist(IEnumerable<object> items, bool stringsAreArtists, int firstPosition)
        {
            int position = firstPosition;

            foreach (object item in items)
            {
                position = AddObjectToPlaylist(item, stringsAreArtists, position);
            }
        }

        private void AddObjectToPlaylist(object o, bool stringsAreArtists)
        {
            if (o is MusicCollectionItem)
            {
                AddObjectToPlaylist((o as MusicCollectionItem).Content, stringsAreArtists);
            }
            else if (o is string)
            {
                if (stringsAreArtists)
                {
                    AddArtistToPlaylist(o as string);
                }
                else
                {
                    AddGenreToPlaylist(o as string);
                }
            }
            else if (o is AlbumMetadata)
            {
                AddAlbumToPlaylist(o as AlbumMetadata);
            }
            else if (o is Playable)
            {
                AddPlayableToPlaylist(o as Playable);
            }
        }

        // Template: firstPosition is the position on the playlist to which
        // the first item is pushed. The return value is the position after
        // the last item.
        private int AddObjectToPlaylist(object o, bool stringsAreArtists, int firstPosition)
        {
            if (o is MusicCollectionItem)
            {
                AddObjectToPlaylist((o as MusicCollectionItem).Content, stringsAreArtists, firstPosition);
            }
            else if (o is string)
            {
                if (stringsAreArtists)
                {
                    return AddArtistToPlaylist(o as string, firstPosition);
                }
                else
                {
                    return AddGenreToPlaylist(o as string, firstPosition);
                }
            }
            else if (o is AlbumMetadata)
            {
                return AddAlbumToPlaylist(o as AlbumMetadata, firstPosition);
            }
            else if (o is Playable)
            {
                return AddPlayableToPlaylist(o as Playable, firstPosition);
            }

            return firstPosition;
        }

        private void AddArtistToPlaylist(string artist)
        {
            foreach (AlbumMetadata album in DataModel.Database.AlbumsByArtist(artist))
            {
                AddAlbumToPlaylist(album);
            }
        }

        private int AddArtistToPlaylist(string artist, int firstPosition)
        {
            int position = firstPosition;

            foreach (AlbumMetadata album in DataModel.Database.AlbumsByArtist(artist))
            {
                position = AddAlbumToPlaylist(album, position);
            }

            return position;
        }

        private void AddGenreToPlaylist(string genre)
        {
            foreach (AlbumMetadata album in DataModel.Database.AlbumsByGenre(genre))
            {
                AddGenreFilteredAlbumToPlaylist(album, genre);
            }
        }

        private int AddGenreToPlaylist(string genre, int firstPosition)
        {
            int position = firstPosition;

            foreach (AlbumMetadata album in DataModel.Database.AlbumsByGenre(genre))
            {
                position = AddGenreFilteredAlbumToPlaylist(album, genre, position);
            }

            return position;
        }

        private void AddAlbumToPlaylist(AlbumMetadata album)
        {
            foreach (SongMetadata song in DataModel.Database.SongsByAlbum(album))
            {
                AddPlayableToPlaylist(song);
            }
        }

        private int AddAlbumToPlaylist(AlbumMetadata album, int firstPosition)
        {
            int position = firstPosition;

            foreach (SongMetadata song in DataModel.Database.SongsByAlbum(album))
            {
                position = AddPlayableToPlaylist(song, position);
            }

            return position;
        }

        private void AddGenreFilteredAlbumToPlaylist(AlbumMetadata album, string genre)
        {
            foreach (SongMetadata song in DataModel.Database.SongsByAlbum(album))
            {
                if (song.Genre == genre)
                {
                    AddPlayableToPlaylist(song);
                }
            }
        }

        private int AddGenreFilteredAlbumToPlaylist(AlbumMetadata album, string genre, int firstPosition)
        {
            int position = firstPosition;

            foreach (SongMetadata song in DataModel.Database.SongsByAlbum(album))
            {
                if (song.Genre == genre)
                {
                    position = AddPlayableToPlaylist(song, position);
                }
            }

            return position;
        }

        private void AddPlayableToPlaylist(Playable playable)
        {
            DataModel.ServerSession.Add(playable.Path);
        }

        private int AddPlayableToPlaylist(Playable playable, int position)
        {
            DataModel.ServerSession.AddId(playable.Path, position);
            return position + 1;
        }

        #endregion

        #endregion

        #region Seek bar

        private bool m_SeekBarIsBeingDragged = false;

        private void OnSeekBarDragStart(object sender, MouseButtonEventArgs e)
        {
            m_SeekBarIsBeingDragged = true;
        }

        private void OnSeekBarValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            m_PlayPosition.Content = Utils.IntToTimecode((int)m_SeekBar.Value);
        }

        private void OnSeekBarDragEnd(object sender, MouseButtonEventArgs e)
        {
            m_SeekBarIsBeingDragged = false;
            DataModel.ServerSession.Seek(DataModel.ServerStatus.CurrentSongIndex, (int)m_SeekBar.Value);
            Update();
        }

        private void OnSeekBarMouseWheel(object sender, MouseWheelEventArgs e)
        {
            int currentPosition = DataModel.ServerStatus.PlayPosition;
            int newPosition = currentPosition;
            int increment = 0;

            if (Settings.Default.MouseWheelAdjustsSongPositionInPercent && Settings.Default.MouseWheelAdjustsSongPositionPercentBy > 0)
            {
                increment = Math.Max(1, Settings.Default.MouseWheelAdjustsSongPositionPercentBy * DataModel.ServerStatus.SongLength / 100);
            }
            else
            {
                increment = Settings.Default.MouseWheelAdjustsSongPositionSecondsBy;
            }

            if (e.Delta < 0)
            {
                newPosition = Math.Max(0, newPosition - increment);
            }
            else if (e.Delta > 0)
            {
                newPosition = Math.Min(DataModel.ServerStatus.SongLength, newPosition + increment);
            }

            if (newPosition != currentPosition)
            {
                DataModel.ServerSession.Seek(DataModel.ServerStatus.CurrentSongIndex, newPosition);
                Update();
            }
        }

        #endregion

        #region Control buttons row

        private void OnBackButtonClicked(object sender, RoutedEventArgs e)
        {
            Back();
        }

        private void OnPlayButtonClicked(object sender, RoutedEventArgs e)
        {
            Play();
        }

        private void OnPauseButtonClicked(object sender, RoutedEventArgs e)
        {
            Pause();
        }

        private void OnPlayPauseButtonClicked(object sender, RoutedEventArgs e)
        {
            TogglePlayPause();
        }

        private void OnStopButtonClicked(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        private void OnSkipButtonClicked(object sender, RoutedEventArgs e)
        {
            Skip();
        }

        bool m_VolumeRestoreInProgress = false;

        private void OnVolumeSliderDragged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!m_PropertyUpdateInProgress && !m_VolumeRestoreInProgress)
            {
                // Volume slider is actually moving because the user is moving it.
                DataModel.ServerSession.SetVol((int)e.NewValue);
            }
        }

        private void OnVolumeMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta < 0)
            {
                VolumeDown();
            }
            else if (e.Delta > 0)
            {
                VolumeUp();
            }
        }

        private void OnToggleRandomClicked(object sender, RoutedEventArgs e)
        {
            DataModel.ServerSession.Random(!DataModel.ServerStatus.IsOnRandom);
            Update();
        }

        private void OnToggleRepeatClicked(object sender, RoutedEventArgs e)
        {
            DataModel.ServerSession.Repeat(!DataModel.ServerStatus.IsOnRepeat);
            Update();
        }

        #endregion

        #region Playback control

        private void Back()
        {
            DataModel.ServerSession.Previous();
            Update();
        }

        private void Play()
        {
            DataModel.ServerSession.Play();
            Update();
        }

        private void Pause()
        {
            DataModel.ServerSession.Pause();
            Update();
        }

        private void TogglePlayPause()
        {
            if (DataModel.ServerStatus != null && DataModel.ServerStatus.OK)
            {
                if (DataModel.ServerStatus.IsPlaying)
                {
                    DataModel.ServerSession.Pause();
                }
                else
                {
                    DataModel.ServerSession.Play();
                }
            }

            Update();
        }

        private void Stop()
        {
            DataModel.ServerSession.Stop();
            Update();
        }

        private void Skip()
        {
            DataModel.ServerSession.Next();
            Update();
        }
                
        private void VolumeDown()
        {
            int? currentVolume = DataModel.ServerStatus.Volume;

            if (currentVolume != null && Settings.Default.EnableVolumeControl)
            {
                int newVolume = Math.Max(0, currentVolume.Value - Settings.Default.VolumeAdjustmentStep);

                if (newVolume != currentVolume)
                {
                    DataModel.ServerSession.SetVol(newVolume);
                    Update();
                }
            }
        }
        
        private void VolumeUp()
        {
            int? currentVolume = DataModel.ServerStatus.Volume;

            if (currentVolume != null && Settings.Default.EnableVolumeControl)
            {
                int newVolume = Math.Min(100, currentVolume.Value + Settings.Default.VolumeAdjustmentStep);

                if (newVolume != currentVolume)
                {
                    DataModel.ServerSession.SetVol(newVolume);
                    Update();
                }
            }
        }

        #endregion

        #region Server Tab

        private void OnUpdateCollectionClicked(object sender, RoutedEventArgs e)
        {
            DataModel.ServerSession.Update();
        }
        
        #endregion

        #region Settings and settings tab

        // Called by SettingsWindow when new settings are applied.
        public void SettingsChanged(bool reconnectNeeded)
        {
            ApplyTabVisibilitySettings();
            DataModel.Servers.SetItems(Servers.ReadFromXml(Settings.Default.Servers), Settings.Default.SelectedServerIndex);
            m_VolumeControl.IsEnabled = DataModel.ServerStatus.Volume.HasValue && Settings.Default.EnableVolumeControl;
            SetTimerInterval(Settings.Default.ViewUpdateInterval);

            StringCollection formatCollection = Settings.Default.AlbumDateFormats;
            IList<string> formatList = new List<string>();

            foreach (string format in formatCollection)
            {
                formatList.Add(format);
            }

            DataModel.CustomDateNormalizer.SetFormats(formatList);

            if (reconnectNeeded)
            {
                DataModel.ServerSession.Disconnect();
            }
        }

        private void ApplyInitialSettings()
        {
            ApplyTabVisibilitySettings();
            DataModel.Servers.SetItems(Servers.ReadFromXml(Settings.Default.Servers), Settings.Default.SelectedServerIndex);
        }

        private void ApplyTabVisibilitySettings()
        {
            m_QuickSearchTab.Visibility = Settings.Default.QuickSearchTabIsVisible ? Visibility.Visible : Visibility.Collapsed;
            m_AdvancedSearchTab.Visibility = Settings.Default.AdvancedTabIsVisible ? Visibility.Visible : Visibility.Collapsed;
            m_ArtistListTab.Visibility = Settings.Default.ArtistListTabIsVisible ? Visibility.Visible : Visibility.Collapsed;
            m_ArtistTreeTab.Visibility = Settings.Default.ArtistTreeTabIsVisible ? Visibility.Visible : Visibility.Collapsed;
            m_GenreListTab.Visibility = Settings.Default.GenreListTabIsVisible ? Visibility.Visible : Visibility.Collapsed;
            m_GenreTreeTab.Visibility = Settings.Default.GenreTreeTabIsVisible ? Visibility.Visible : Visibility.Collapsed;
            m_FilesystemTab.Visibility = Settings.Default.FilesystemTabIsVisible ? Visibility.Visible : Visibility.Collapsed;
            m_StreamsTab.Visibility = Settings.Default.StreamsTabIsVisible ? Visibility.Visible : Visibility.Collapsed;
            m_PlaylistsTab.Visibility = Settings.Default.PlaylistsTabIsVisible ? Visibility.Visible : Visibility.Collapsed;

            if (Settings.Default.DefaultMusicCollectionTab == MusicCollectionTab.QuickSearchTab.ToString())
            {
                m_QuickSearchTab.Visibility = Visibility.Visible;
                m_QuickSearchTab.IsSelected = true;
            }
            else if (Settings.Default.DefaultMusicCollectionTab == MusicCollectionTab.ArtistListTab.ToString())
            {
                m_ArtistListTab.Visibility = Visibility.Visible;
                m_ArtistListTab.IsSelected = true;
            }
            else if (Settings.Default.DefaultMusicCollectionTab == MusicCollectionTab.ArtistTreeTab.ToString())
            {
                m_ArtistTreeTab.Visibility = Visibility.Visible;
                m_ArtistTreeTab.IsSelected = true;
            }
            else if (Settings.Default.DefaultMusicCollectionTab == MusicCollectionTab.GenreListTab.ToString())
            {
                m_GenreListTab.Visibility = Visibility.Visible;
                m_GenreListTab.IsSelected = true;
            }
            else if (Settings.Default.DefaultMusicCollectionTab == MusicCollectionTab.GenreTreeTab.ToString())
            {
                m_GenreTreeTab.Visibility = Visibility.Visible;
                m_GenreTreeTab.IsSelected = true;
            }
            else if (Settings.Default.DefaultMusicCollectionTab == MusicCollectionTab.FilesystemTab.ToString())
            {
                m_FilesystemTab.Visibility = Visibility.Visible;
                m_FilesystemTab.IsSelected = true;
            }
            else if (Settings.Default.DefaultMusicCollectionTab == MusicCollectionTab.AdvancedSearchTab.ToString())
            {
                m_AdvancedSearchTab.Visibility = Visibility.Visible;
                m_AdvancedSearchTab.IsSelected = true;
            }
            else if (Settings.Default.DefaultMusicCollectionTab == MusicCollectionTab.StreamsTab.ToString())
            {
                m_StreamsTab.Visibility = Visibility.Visible;
                m_StreamsTab.IsSelected = true;
            }
            else if (Settings.Default.DefaultMusicCollectionTab == MusicCollectionTab.PlaylistsTab.ToString())
            {
                m_PlaylistsTab.Visibility = Visibility.Visible;
                m_PlaylistsTab.IsSelected = true;
            }
        }

        #endregion

        #region String query overlay use cases

        #region Querying for a new stream address and name

        private void StartAddNewStreamQuery()
        {
            m_Overlay.Activate("Enter the address of the new stream:", "http://", OnAddStreamAddressOverlayReturned);
        }
        
        private void OnAddStreamAddressOverlayReturned(bool ok, string input)
        {
            string address = input.Trim();

            if (ok && address.Length > 0)
            {
                m_Overlay.Activate("Enter a name for this stream:", "", OnAddStreamNameOverlayReturned, address);
            }
            else
            {
                m_Overlay.Deactivate();
            }
        }

        private void OnAddStreamNameOverlayReturned(bool ok, string input)
        {
            string name = input.Trim();

            if (ok && name.Length > 0)
            {
                StreamMetadata stream = new StreamMetadata(m_Overlay.Data as string, name);
                DataModel.StreamsCollection.Add(stream);
            }
            
            m_Overlay.Deactivate();
        }

        #endregion

        #region Querying for a new name for a stream

        private void StartRenameStreamQuery()
        {
            if (m_StreamsView.SelectedItems.Count == 1)
            {
                StreamMetadata stream = (m_StreamsView.SelectedItem as MusicCollectionItem).Content as StreamMetadata;
                m_Overlay.Activate("New stream name:", stream.Label, OnRenameStreamOverlayReturned, stream);
            }
        }

        private void OnRenameStreamOverlayReturned(bool ok, string input)
        {
            string trimmedName = input.Trim();

            if (ok && trimmedName.Length > 0)
            {
                DataModel.StreamsCollection.Rename(m_Overlay.Data as StreamMetadata, trimmedName);
            }

            m_Overlay.Deactivate();
        }

        #endregion

        #region Querying for a name for a new playlist

        private void StartAddNewPlaylistAsQuery(string currentPlaylistName)
        {
            m_Overlay.Activate("Save this playlist on the server as:", currentPlaylistName, OnSavePlaylistAsOverlayReturned);
        }

        private void OnSavePlaylistAsOverlayReturned(bool ok, string input)
        {
            string trimmedName = input.Trim();

            if (ok && trimmedName.Length > 0)
            {
                DataModel.SavedPlaylists.CurrentPlaylistName = trimmedName;
                DataModel.ServerSession.Rm(DataModel.SavedPlaylists.CurrentPlaylistName);
                DataModel.ServerSession.Save(DataModel.SavedPlaylists.CurrentPlaylistName);
                DataModel.SavedPlaylists.Refresh();
            }

            m_Overlay.Deactivate();
        }

        #endregion

        #region Querying for a new name for a previously saved playlist

        private void StartRenameSavedPlaylistQuery(string oldName)
        {
            m_Overlay.Activate("New playlist name:", oldName, OnRenameSavedPlaylistOverlayReturned, oldName);
        }

        private void OnRenameSavedPlaylistOverlayReturned(bool ok, string input)
        {
            string trimmedName = input.Trim();

            if (ok && trimmedName.Length > 0)
            {
                DataModel.ServerSession.Rename(m_Overlay.Data as string, trimmedName);
                DataModel.SavedPlaylists.CurrentPlaylistName = trimmedName;
                DataModel.SavedPlaylists.Refresh();
            }

            m_Overlay.Deactivate();
        }

        #endregion

        #endregion

        #region Child window handling

        private void BringUpSettingsWindow()
        {
            if (m_SettingsWindow == null)
            {
                m_SettingsWindow = new SettingsWindow(DataModel);
            }
            else
            {
                m_SettingsWindow.Visibility = Visibility.Visible;
            }

            m_SettingsWindow.Show();
        }

        private void BringUpLicenseWindow()
        {
            if (m_LicenseWindow == null)
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                Stream stream = assembly.GetManifestResourceStream("Auremo.Text.LICENSE.txt");
                StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                string license = reader.ReadToEnd();

                m_LicenseWindow = new TextWindow("License - Auremo MPD Client", license, this);
            }
            else
            {
                m_LicenseWindow.Visibility = Visibility.Visible;
            }

            m_LicenseWindow.Show();
        }

        private void BringUpAboutWindow()
        {
            if (m_AboutWindow == null)
            {
                m_AboutWindow = new AboutWindow(this);
            }
            else
            {
                m_AboutWindow.Visibility = Visibility.Visible;
            }

            m_AboutWindow.Show();
        }

        public void OnChildWindowClosing(Window window)
        {
            if (window == m_AboutWindow)
            {
                m_AboutWindow = null;
            }
            else if (window == m_LicenseWindow)
            {
                m_LicenseWindow = null;
            }
            else if (window == m_SettingsWindow)
            {
                m_SettingsWindow = null;
            }
        }

        #endregion

        #region Miscellaneous helper functions
        
        private DataGridRow DataGridRowBeingClicked(DataGrid grid, MouseButtonEventArgs e)
        {
            HitTestResult hit = VisualTreeHelper.HitTest(grid, e.GetPosition(grid));

            if (hit != null)
            {
                DependencyObject component = (DependencyObject)hit.VisualHit;

                while (component != null)
                {
                    if (component is DataGridRow)
                    {
                        return (DataGridRow)component;
                    }
                    else
                    {
                        component = VisualTreeHelper.GetParent(component);
                    }
                }
            }

            return null;
        }

        private TreeViewItem TreeViewItemBeingClicked(TreeView tree, MouseButtonEventArgs e)
        {
            HitTestResult hit = VisualTreeHelper.HitTest(tree, e.GetPosition(tree));

            if (hit != null)
            {
                DependencyObject component = (DependencyObject)hit.VisualHit;

                if (component is TextBlock) // Don't return hits to the expander arrow.
                {
                    while (component != null)
                    {
                        if (component is TreeViewItem)
                        {
                            return (TreeViewItem)component;
                        }
                        else
                        {
                            component = VisualTreeHelper.GetParent(component);
                        }
                    }
                }
            }

            return null;
        }

        private bool ViewBackgroundWasClicked(Control control, MouseButtonEventArgs e)
        {
            HitTestResult hit = VisualTreeHelper.HitTest(control, e.GetPosition(control));

            if (hit != null)
            {
                DependencyObject component = (DependencyObject)hit.VisualHit;

                while (component != null)
                {
                    // The expander button in a tree view or the scrollbar of
                    // any view -> stop here. There must be other such end
                    // conditions, so this is by no means a generally corrent
                    // solution. It's good enough here though.
                    if (component is ToggleButton || component is ScrollBar || component is Thumb)
                    {
                        return false;
                    }
                    else if (component is ItemsControl)
                    {
                        return true;
                    }
                    else
                    {
                        component = VisualTreeHelper.GetParent(component);
                    }
                }
            }

            return false;
        }

        private TreeViewController TreeViewControllerOf(TreeView tree)
        {
            if (tree == m_ArtistTree)
            {
                return DataModel.DatabaseView.ArtistTreeController;
            }
            else if (tree == m_GenreTree)
            {
                return DataModel.DatabaseView.GenreTreeController;
            }
            else if (tree == m_DirectoryTree)
            {
                return DataModel.DatabaseView.DirectoryTreeController;
            }

            throw new Exception("Tried to find the controller of an unknown TreeView.");
        }

        private int DropTargetRowIndex(DragEventArgs e)
        {
            for (int i = 0; i < m_PlaylistView.Items.Count; ++i)
            {
                DataGridRow row = m_PlaylistView.ItemContainerGenerator.ContainerFromIndex(i) as DataGridRow;

                if (row != null)
                {
                    Point pt = e.GetPosition(row);
                    double yCoord = row.TranslatePoint(pt, row).Y;
                    double halfHeight = row.ActualHeight / 2;

                    if (yCoord < halfHeight)
                    {
                        return i;
                    }
                }
            }

            return m_PlaylistView.Items.Count;
        }

        private string GetDragDropDataString(object dragSource)
        {
            if (dragSource == m_ArtistsView)
            {
                return AddArtists;
            }
            else if (dragSource == m_GenresView)
            {
                return AddGenres;
            }
            else if (dragSource == m_AlbumsBySelectedArtistsView || dragSource == m_AlbumsOfSelectedGenresView)
            {
                return AddAlbums;
            }
            else if (dragSource == m_QuickSearchResultsView || dragSource == m_AdvancedSearchResultsView || dragSource == m_SongsOnSelectedAlbumsView || dragSource == m_SongsOnSelectedGenreAlbumsView)
            {
                return AddSongs;
            }
            else if (dragSource == m_SavedPlaylistsView)
            {
                return LoadPlaylist;
            }
            else if (dragSource is TreeView)
            {
                return AddSongs;
            }
            else if (dragSource == m_StreamsView)
            {
                return AddStreams;
            }
            else if (dragSource == m_ItemsOnSelectedSavedPlaylistView)
            {
                return AddPlaylistItems;
            }
            else if (dragSource == m_PlaylistView)
            {
                return MovePlaylistItems;
            }

            throw new Exception("GetDragDropDataString: unknown drag source.");
        }

        private bool EnsureTreeViewHasCurrentNode(TreeViewController controller)
        {
            if (controller.Current == null)
            {
                controller.Current = controller.FirstNode;
                return controller.Current != null;
            }

            return true;
        }

        // nodeContainer must be either a TreeView or a TreeViewItem.
        private TreeViewItem GetTreeViewItem(ItemsControl nodeContainer, TreeViewNode node)
        {
            if (nodeContainer == null || node == null)
            {
                return null;
            }
            else
            {
                TreeViewItem nodeWithHighestLowerID = null;
                TreeViewItem item = null;
                int i = 0;

                do
                {
                    nodeWithHighestLowerID = item;
                    item = nodeContainer.ItemContainerGenerator.ContainerFromIndex(i++) as TreeViewItem;
                } while (item != null && ((TreeViewNode)item.Header).ID < node.ID);

                if (item != null && ((TreeViewNode)item.Header).ID == node.ID)
                {
                    return item;
                }
                else
                {
                    return GetTreeViewItem(nodeWithHighestLowerID, node);
                }
            }
        }

        #endregion
    }
}
