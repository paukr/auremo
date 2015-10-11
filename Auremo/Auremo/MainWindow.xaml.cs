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

using Auremo.GUI;
using Auremo.MusicLibrary;
using Auremo.Properties;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

using Path = Auremo.MusicLibrary.Path;

namespace Auremo
{
    public partial class MainWindow : Window
    {
        private SettingsWindow m_SettingsWindow = null;
        private CoverArtWindow m_CoverArtWindow = null;
        private TextWindow m_LicenseWindow = null;
        private AboutWindow m_AboutWindow = null;
        private DispatcherTimer m_Timer = null;
        private object m_DragSource = null;
        private IList<LibraryItem> m_DragDropPayload = null;
        private Point? m_DragStartPosition = null;
        private bool m_PropertyUpdateInProgress = false;
        private string m_AutoSearchString = "";
        private DateTime m_TimeOfLastAutoSearch = DateTime.MinValue;
        private object m_LastAutoSearchSender = null;
        private object m_LastLastAutoSearchHit = null;

        private const int m_AutoSearchMaxKeystrokeGap = 2500;

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
            AssociateTreeAndController(m_ArtistTree, DataModel.DatabaseView.ArtistTreeController);
            AssociateTreeAndController(m_GenreTree, DataModel.DatabaseView.GenreTreeController);
            AssociateTreeAndController(m_DirectoryTree, DataModel.DatabaseView.DirectoryTreeController);
        }



        private void CreateTimer(int interval)
        {
            m_Timer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher);
            m_Timer.Tick += new EventHandler(OnTimerTick);
            m_Timer.Interval = new TimeSpan(0, 0, 0, 0, interval);
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

        private void OnSelectedSavedPlaylistChanged(object sender, SelectionChangedEventArgs e)
        {
            DataModel.SavedPlaylists.OnSelectedSavedPlaylistChanged();
        }

        private void OnSelectedPlaylistItemsChanged(object sender, SelectionChangedEventArgs e)
        {
            DataModel.Playlist.OnSelectedItemsChanged();
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
                else if (e.Key == Key.Space && !m_QuickSearchBox.IsKeyboardFocused && !m_AdvancedSearchBox.IsKeyboardFocused && !AutoSearchInProgress)
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
            DataModel.CoverArtRepository.Terminate();
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

        private void OnCrossfadeClick(object sender, RoutedEventArgs e)
        {
            m_Overlay.Activate("Crossfade duration (seconds):", DataModel.ServerStatus.Crossfade.ToString(), OnCrossfadeOverlayReturned);
        }

        private void OnMixRampdbClick(object sender, RoutedEventArgs e)
        {
            m_Overlay.Activate("Mix ramp threshold (dB), empty to disable:", DataModel.ServerStatus.MixRampdb.ToString(), OnMixRampdbOverlayReturned);
        }

        private void OnMixRampDelayClick(object sender, RoutedEventArgs e)
        {
            double oldValue = DataModel.ServerStatus.MixRampDelay;
            string parameter = double.IsNaN(oldValue) ? "" : oldValue.ToString(NumberFormatInfo.InvariantInfo);
            m_Overlay.Activate("Mix ramp delay (seconds), empty to disable:", parameter, OnMixRampDelayOverlayReturned);
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

        private void OnMusicCollectionDataGridKeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Handled)
            {
                if (e.Key == Key.Enter)
                {
                    DataGrid grid = sender as DataGrid;
                    SendItemsToPlaylist(grid.Selection());
                    e.Handled = true;
                }
                else if (sender == m_StreamsView)
                {
                    OnStreamsViewKeyDown(sender, e);
                }
            }
        }

        private void SendItemsToPlaylist(IEnumerable<LibraryItem> items)
        {
            if (Settings.Default.SendToPlaylistMethod == SendToPlaylistMethod.AddAsNext.ToString())
            {
                AddItemsToPlaylist(items, DataModel.ServerStatus.CurrentSongIndex + 1);
            }
            else if (Settings.Default.SendToPlaylistMethod == SendToPlaylistMethod.ReplaceAndPlay.ToString())
            {
                DataModel.ServerSession.Clear();
                AddItemsToPlaylist(items);
                DataModel.ServerSession.Play();
            }
            else // Assume SendToPlaylistMethod.Append as the default
            {
                AddItemsToPlaylist(items);
            }

            Update();
        }

        private void AddItemsToPlaylist(IEnumerable<LibraryItem> items)
        {
            foreach (LibraryItem item in items)
            {
                if (item is Playable)
                {
                    DataModel.ServerSession.Add((item as Playable).Path.Full);
                }
                else
                {
                    AddItemsToPlaylist(DataModel.Database.Expand(item));
                }
            }
        }

        private int AddItemsToPlaylist(IEnumerable<LibraryItem> items, int startingPosition)
        {
            int position = startingPosition;

            foreach (LibraryItem item in items)
            {
                if (item is Playable)
                {
                    DataModel.ServerSession.AddId((item as Playable).Path.Full, position++);
                }
                else
                {
                    position = AddItemsToPlaylist(DataModel.Database.Expand(item), position);
                }
            }

            return position;
        }

        private void OnCollectionTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!e.Handled && e.Text != null && e.Text.Length == 1)
            {
                CollectionAutoSearch(sender, e.Text[0]);
            }
        }

        private void OnMusicCollectionDataGridDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            if (!e.Handled && Keyboard.Modifiers == ModifierKeys.None && SenderIsNotScrollbar(e))
            {
                SendItemsToPlaylist((sender as DataGrid).Selection());
                e.Handled = true;
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

            if (senderView is DataGrid)
            {
                DataGrid list = senderView as DataGrid;
                AddItemsToPlaylist(list.Selection(), insertPosition);
            }
            else if (senderView is TreeView)
            {
                HierarchyController controller = GetControllerOf(senderView as TreeView);
                AddItemsToPlaylist(controller.SelectedLeaves, insertPosition);
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
            Settings.Default.Servers = DataModel.ServerList.Serialize();
            Settings.Default.Save();
        }

        private IList<Song> SelectedLocalSongsOnPlaylist()
        {
            IList<Song> result = new List<Song>();

            foreach (PlaylistItem selectedItem in DataModel.Playlist.Items.SelectedItems<PlaylistItem>())
            {
                Song song = null;

                if (DataModel.Database.Songs.TryGetValue(selectedItem.Path, out song))
                {
                    result.Add(song);
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
                            int pivotIndex = (dataGrid.CurrentItem as IndexedLibraryItem).Position;
                            int clickedIndex = (row.Item as IndexedLibraryItem).Position;
                            int minIndex = Math.Min(pivotIndex, clickedIndex);
                            int maxIndex = Math.Max(pivotIndex, clickedIndex);

                            foreach (IndexedLibraryItem item in dataGrid.Items.Cast<IndexedLibraryItem>())
                            {
                                item.IsSelected = item.Position >= minIndex && item.Position <= maxIndex;
                            }
                        }

                        e.Handled = true;
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
                    // Commence drag+drop.
                    m_DragDropPayload = null;

                    if (m_DragSource is DataGrid)
                    {
                        m_DragDropPayload = (m_DragSource as DataGrid).Selection();
                    }
                    else if (m_DragSource is TreeView)
                    {
                        HierarchyController controller = GetControllerOf(m_DragSource as TreeView);
                        m_DragDropPayload = controller.SelectedLeaves.ToList();
                    }

                    if (m_DragDropPayload != null && m_DragDropPayload.Count() > 0)
                    {
                        DragDropEffects mode = sender == m_PlaylistView ? DragDropEffects.Move : DragDropEffects.Copy;
                        m_MousePointerHint.Content = DragDropPayloadDescription();
                        DragDrop.DoDragDrop((DependencyObject)sender, m_DragDropPayload, mode);
                    }
                    else
                    {
                        m_DragDropPayload = null;
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

                result.Append(m_DragDropPayload[i].DisplayString);
            }

            if (m_DragDropPayload.Count > nameLines)
            {
                result.Append("\n+" + (m_DragDropPayload.Count - nameLines) + " more...");
            }

            return result.ToString();
        }

        private void OnPlaylistViewDragOver(object sender, DragEventArgs e)
        {
            m_MousePointerHint.IsOpen = true;
            m_MousePointerHint.Visibility = Visibility.Visible;

            if (!m_PlaylistView.Items.IsEmpty)
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

        private void OnPlaylistViewDragLeave(object sender, DragEventArgs e)
        {
            m_MousePointerHint.IsOpen = false;
            m_MousePointerHint.Visibility = Visibility.Hidden;
            m_DropPositionIndicator.Visibility = Visibility.Hidden;
        }

        private void OnPlaylistViewDrop(object sender, DragEventArgs e)
        {
            int targetRow = DropTargetRowIndex(e);

            if (m_DragDropPayload[0] is SavedPlaylist)
            {
                LoadSavedPlaylist((m_DragDropPayload[0] as SavedPlaylist).Title);
            }
            else if (m_DragDropPayload[0] is PlaylistItem)
            {
                foreach (PlaylistItem item in m_DragDropPayload)
                {
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
            else
            {
                AddItemsToPlaylist(m_DragDropPayload, targetRow);
            }

            Update();

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
                    GetControllerOf(tree).ClearMultiSelection();
                    e.Handled = true;
                }
            }
            else if (item.Header is HierarchicalLibraryItem)
            {
                HierarchicalLibraryItem node = item.Header as HierarchicalLibraryItem;

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
                        SendItemsToPlaylist(node.LeafItems);
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

                if (item != null && item.Header is HierarchicalLibraryItem)
                {
                    HierarchicalLibraryItem node = item.Header as HierarchicalLibraryItem;
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
                HierarchyController controller = GetControllerOf(tree);

                if (Keyboard.Modifiers == ModifierKeys.None || Keyboard.Modifiers == ModifierKeys.Shift)
                {
                    bool currentChanged = false;

                    if (e.Key == Key.Up && controller.CurrentOrFirstNode != null)
                    {
                        controller.Current = controller.Previous;
                        currentChanged = true;
                        e.Handled = true;
                    }
                    else if (e.Key == Key.Down && controller.CurrentOrFirstNode != null)
                    {
                        controller.Current = controller.Next;
                        currentChanged = true;
                        e.Handled = true;
                    }
                    else if (e.Key == Key.Left && controller.CurrentOrFirstNode != null)
                    {
                        if (controller.Current.Parent != null)
                        {
                            controller.Current = controller.Current.Parent;
                            currentChanged = true;
                        }

                        e.Handled = true;
                    }
                    else if (e.Key == Key.Right && controller.CurrentOrFirstNode != null)
                    {
                        if (controller.Current.Children.Count > 0)
                        {
                            controller.Current.IsExpanded = true;
                            controller.Current = controller.Current.Children[0];
                            currentChanged = true;
                        }

                        e.Handled = true;
                    }
                    else if (e.Key == Key.Enter)
                    {
                        SendItemsToPlaylist(controller.SelectedLeaves);
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
            HierarchicalLibraryItem node = e.NewValue as HierarchicalLibraryItem;

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
            if (e.Key == Key.F2)
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
                List<AudioStream> streamsToAdd = new List<AudioStream>();

                foreach (string filename in dialog.FileNames)
                {
                    IEnumerable<AudioStream> streams = null;

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
                string playlist = PlaylistWriter.Write(m_StreamsView.Selection());

                if (playlist != null)
                {
                    File.WriteAllText(filename, playlist);
                }
            }
        }

        private void DeleteSelectedStreams()
        {
            DataModel.StreamsCollection.Delete(DataModel.StreamsCollection.Streams.SelectedItems<AudioStream>());
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
            if (Keyboard.Modifiers == ModifierKeys.None && SenderIsNotScrollbar(e))
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
            IndexedLibraryItem selection = m_SavedPlaylistsView.SelectedItem as IndexedLibraryItem;

            if (selection != null)
            {
                LoadSavedPlaylist((selection.Item as SavedPlaylist).Title);
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
            SavedPlaylist selection = DataModel.SavedPlaylists.SelectedSavedPlaylist;

            if (selection != null)
            {
                StartRenameSavedPlaylistQuery(selection.Title);
            }
        }

        private void OnDeleteSavedPlaylistClicked(object sender, RoutedEventArgs e)
        {
            DeleteSelectedSavedPlaylist();
        }

        private void DeleteSelectedSavedPlaylist()
        {
            SavedPlaylist selection = DataModel.SavedPlaylists.SelectedSavedPlaylist;

            if (selection != null)
            {
                DataModel.ServerSession.Rm(selection.Title);
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
                    IEnumerable<PlaylistItem> selection = m_PlaylistView.Selection().Cast<PlaylistItem>();

                    if (selection.Count() == 1)
                    {
                        DataModel.ServerSession.PlayId(selection.First().Id);
                        Update();
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
            if (Keyboard.Modifiers == ModifierKeys.None && SenderIsNotScrollbar(e))
            {
                DataGridRow row = DataGridRowBeingClicked(m_PlaylistView, e);

                if (row != null)
                {
                    IndexedLibraryItem genericItem = row.Item as IndexedLibraryItem;
                    PlaylistItem playlistItem = genericItem.Item as PlaylistItem;
                    DataModel.ServerSession.PlayId(playlistItem.Id);
                    Update();
                }
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
                foreach (IndexedLibraryItem row in DataModel.Playlist.Items)
                {
                    if (!row.IsSelected)
                    {
                        DataModel.ServerSession.DeleteId((row.Item as PlaylistItem).Id);
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
            ISet<Path> paths = new SortedSet<Path>();

            foreach (IndexedLibraryItem row in DataModel.Playlist.Items)
            {
                PlaylistItem item = row.Item as PlaylistItem;

                if (paths.Contains(item.Path))
                {
                    DataModel.ServerSession.DeleteId(item.Id);
                }
                else
                {
                    paths.Add(item.Path);
                }
            }
        }

        private void OnShufflePlaylistClicked(object sender, RoutedEventArgs e)
        {
            DataModel.ServerSession.Shuffle();
        }

        private void OnShowInArtistsListClicked(object sender, RoutedEventArgs e)
        {
            m_ArtistListTab.IsSelected = true;
            DataModel.DatabaseView.ShowInArtistList(DataModel.Playlist.Items.SelectedItems().Cast<Playable>());
        }

        private void OnShowInArtistsTreeClicked(object sender, RoutedEventArgs e)
        {
            m_ArtistTreeTab.IsSelected = true;
            DataModel.DatabaseView.ShowInArtistTree(DataModel.Playlist.Items.SelectedItems().Cast<Playable>());
        }

        private void OnShowInGenreListClicked(object sender, RoutedEventArgs e)
        {
            m_GenreListTab.IsSelected = true;
            DataModel.DatabaseView.ShowInGenreList(DataModel.Playlist.Items.SelectedItems().Cast<Playable>());
        }

        private void OnShowInGenreTreeClicked(object sender, RoutedEventArgs e)
        {
            m_GenreTreeTab.IsSelected = true;
            DataModel.DatabaseView.ShowInGenreTree(DataModel.Playlist.Items.SelectedItems().Cast<Playable>());
        }

        private void OnShowInFilesystemTreeClicked(object sender, RoutedEventArgs e)
        {
            m_FilesystemTab.IsSelected = true;
            DataModel.DatabaseView.ShowInDirectoryTree(DataModel.Playlist.Items.SelectedItems().Cast<Playable>());
        }

        private void DeleteSelectedItemsFromPlaylist()
        {
            foreach (PlaylistItem item in m_PlaylistView.Selection().Cast<PlaylistItem>())
            {
                DataModel.ServerSession.DeleteId(item.Id);
            }

            Update();
        }

        #endregion

        #region Autosearch

        private bool AutoSearchInProgress
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
                HierarchyController controller = GetControllerOf(sender as TreeView);
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

                    foreach (IndexedLibraryItem item in grid.Items.Cast<IndexedLibraryItem>())
                    {
                        if (item.Item.DisplayString.ToLowerInvariant().StartsWith(m_AutoSearchString))
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
                    HierarchicalLibraryItem item = CollectionAutoSearchTreeViewRecursively(tree.Items.Cast<HierarchicalLibraryItem>());

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

        private HierarchicalLibraryItem CollectionAutoSearchTreeViewRecursively(IEnumerable<HierarchicalLibraryItem> nodes)
        {
            foreach (HierarchicalLibraryItem node in nodes)
            {
                if (node.DisplayString.ToLowerInvariant().StartsWith(m_AutoSearchString))
                {
                    return node;
                }
                else if (node.IsExpanded)
                {
                    HierarchicalLibraryItem result = CollectionAutoSearchTreeViewRecursively(node.Children);

                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            return null;
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
            DataModel.ServerList.Deserialize(Settings.Default.Servers);
            m_VolumeControl.IsEnabled = DataModel.ServerStatus.Volume.HasValue && Settings.Default.EnableVolumeControl;
            m_Timer.Interval = new TimeSpan(0, 0, 0, 0, Settings.Default.ViewUpdateInterval);
            DataModel.CustomDateNormalizer.ReadFromSettings();

            if (reconnectNeeded)
            {
                DataModel.ServerSession.Disconnect();
            }
        }

        private void ApplyInitialSettings()
        {
            ApplyTabVisibilitySettings();
            DataModel.ServerList.Deserialize(Settings.Default.Servers);
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
            string label = input.Trim();

            if (ok && label.Length > 0)
            {
                string pathString = m_Overlay.Data as string;
                AudioStream stream = new AudioStream(new Path(pathString), label);
                DataModel.StreamsCollection.Add(stream);
            }
            
            m_Overlay.Deactivate();
        }

        #endregion

        #region Querying for a new name for a stream

        private void StartRenameStreamQuery()
        {
            IEnumerable<AudioStream> selection = DataModel.StreamsCollection.Streams.SelectedItems().Cast<AudioStream>();

            if (selection.Count() == 1)
            {
                AudioStream stream = selection.First();
                m_Overlay.Activate("New stream name:", stream.Label, OnRenameStreamOverlayReturned, stream);
            }
        }

        private void OnRenameStreamOverlayReturned(bool ok, string input)
        {
            string trimmedName = input.Trim();

            if (ok && trimmedName.Length > 0)
            {
                DataModel.StreamsCollection.Rename(m_Overlay.Data as AudioStream, trimmedName);
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

        #region Crossfade/mix ramp queries

        private void OnCrossfadeOverlayReturned(bool ok, string input)
        {
            if (ok)
            {
                int? newValue = Utils.StringToInt(input);

                if (newValue.HasValue)
                {
                    DataModel.ServerSession.Crossfade(newValue.Value);
                    Update();
                }
            }

            m_Overlay.Deactivate();
        }

        private void OnMixRampdbOverlayReturned(bool ok, string input)
        {
            if (ok)
            {
                if (input.Length == 0)
                {
                    DataModel.ServerSession.MixRampDelay(double.NaN);
                }
                else
                {
                    double? newValue = Utils.StringToDouble(input);

                    if (newValue.HasValue)
                    {
                        DataModel.ServerSession.MixRampdb(newValue.Value);
                        Update();
                    }
                }
            }

            m_Overlay.Deactivate();
        }

        private void OnMixRampDelayOverlayReturned(bool ok, string input)
        {
            if (ok)
            {
                if (input.Length == 0)
                {
                    DataModel.ServerSession.MixRampDelay(double.NaN);
                }
                else
                {
                    int? newValue = Utils.StringToInt(input);

                    if (newValue.HasValue)
                    {
                        DataModel.ServerSession.MixRampDelay(newValue.Value);
                        Update();
                    }
                }
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

        private void BringUpCoverArtWindow()
        {
            if (m_CoverArtWindow == null)
            {
                m_CoverArtWindow = new CoverArtWindow(this);
            }
            else
            {
                m_CoverArtWindow.Visibility = Visibility.Visible;
            }

            m_CoverArtWindow.Show();
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

        // Return the TreeViewItem that contains item. nodeContainer must be either a TreeView or a TreeViewItem.
        private TreeViewItem GetTreeViewItem(ItemsControl nodeContainer, HierarchicalLibraryItem node)
        {
            if (nodeContainer == null || node == null)
            {
                return null;
            }
            else
            {
                TreeViewItem nodeWithHighestLowerId = null;
                TreeViewItem item = null;
                int i = 0;

                do
                {
                    nodeWithHighestLowerId = item;
                    item = nodeContainer.ItemContainerGenerator.ContainerFromIndex(i++) as TreeViewItem;
                } while (item != null && ((HierarchicalLibraryItem)item.Header).Id < node.Id);

                if (item != null && ((HierarchicalLibraryItem)item.Header).Id == node.Id)
                {
                    return item;
                }
                else
                {
                    return GetTreeViewItem(nodeWithHighestLowerId, node);
                }
            }
        }

        private void AssociateTreeAndController(TreeView tree, HierarchyController controller)
        {
            tree.Tag = controller;
        }

        private HierarchyController GetControllerOf(TreeView tree)
        {
            return tree.Tag as HierarchyController;
        }

        /// Verifies that a double-click was not sent by DataGrid's scrollbar.
        /// Trust Microsoft to mess up stuff like this.
        private bool SenderIsNotScrollbar(MouseEventArgs e)
        {
            return e.OriginalSource is TextBlock;
        }

        #endregion
    }
}
