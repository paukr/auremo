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

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Auremo.Properties;

namespace Auremo
{
    public partial class SettingsWindow : Window, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        #endregion

        DataModel m_DataModel = null;
        const char m_StringCollectionSeparator = ';';

        public SettingsWindow(DataModel dataModel)
        {
            InitializeComponent();
            ServerList = new ServerList();
            DataContext = this;
            m_DataModel = dataModel;
            LoadSettings();

            ServerList.PropertyChanged += new PropertyChangedEventHandler(OnServerListChanged);
        }

        private void OnNumericOptionPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = new Regex("[^0-9]+").IsMatch(e.Text);
        }

        private void ValidateOptions(object sender, RoutedEventArgs e)
        {
            ValidateOptions();
        }

        private void ValidateOptions()
        {
            ClampTextBoxContent(m_PortEntry, 1, 6600, 65536);
            ClampTextBoxContent(m_UpdateIntervalEntry, 100, 500, 5000);
            ClampTextBoxContent(m_NetworkTimeoutEntry, 1, 10, 600);
            ClampTextBoxContent(m_ReconnectIntervalEntry, 0, 10, 3600);
            ClampTextBoxContent(m_WheelVolumeStepEntry, 0, 5, 100);
            ClampTextBoxContent(m_WheelSongPositioningPercentEntry, 0, 5, 100);
            ClampTextBoxContent(m_WheelSongPositioningSecondsEntry, 0, 5, 1800);
        }

        private void ClampTextBoxContent(object control, int min, int dfault, int max)
        {
            TextBox textBox = control as TextBox;
            int value = Utils.StringToInt(textBox.Text, dfault);
            textBox.Text = Utils.Clamp(min, value, max).ToString();
        }

        private void OnCancelClicked(object sender, RoutedEventArgs e)
        {
            LoadSettings();
            Close();
        }

        private void OnRevertClicked(object sender, RoutedEventArgs e)
        {
            LoadSettings();
        }

        private void OnApplyClicked(object sender, RoutedEventArgs e)
        {
            SaveSettings();
        }

        private void OnOKClicked(object sender, RoutedEventArgs e)
        {
            ServerEntryUpdated(null, null);
            SaveSettings();
            Close();
        }

        private void LoadSettings()
        {
            m_UpdateIntervalEntry.Text = Settings.Default.ViewUpdateInterval.ToString();
            m_NetworkTimeoutEntry.Text = Settings.Default.NetworkTimeout.ToString();
            m_ReconnectIntervalEntry.Text = Settings.Default.ReconnectInterval.ToString();
            m_WheelVolumeStepEntry.Text = Settings.Default.VolumeAdjustmentStep.ToString();
            m_WheelSongPositioningModeIsPercent.IsChecked = Settings.Default.MouseWheelAdjustsSongPositionInPercent;
            m_WheelSongPositioningModeIsSeconds.IsChecked = !m_WheelSongPositioningModeIsPercent.IsChecked;
            m_WheelSongPositioningPercentEntry.Text = Settings.Default.MouseWheelAdjustsSongPositionPercentBy.ToString();
            m_WheelSongPositioningSecondsEntry.Text = Settings.Default.MouseWheelAdjustsSongPositionSecondsBy.ToString();
            m_EnableVolumeControl.IsChecked = Settings.Default.EnableVolumeControl;
            m_UseAlbumArtist.IsChecked = Settings.Default.UseAlbumArtist;
            m_SortAlbumsByDate.IsChecked = Settings.Default.AlbumSortingMode == AlbumSortingMode.ByDate.ToString();
            m_SortAlbumsByName.IsChecked = m_SortAlbumsByDate.IsChecked != true;
            m_QuickSearchTabIsVisible.IsChecked = Settings.Default.QuickSearchTabIsVisible;
            m_AdvancedSearchTabIsVisible.IsChecked = Settings.Default.AdvancedTabIsVisible;
            m_ArtistListTabIsVisible.IsChecked = Settings.Default.ArtistListTabIsVisible;
            m_ArtistTreeTabIsVisible.IsChecked = Settings.Default.ArtistTreeTabIsVisible;
            m_GenreListTabIsVisible.IsChecked = Settings.Default.GenreListTabIsVisible;
            m_GenreTreeTabIsVisible.IsChecked = Settings.Default.GenreTreeTabIsVisible;
            m_FilesystemTabIsVisible.IsChecked = Settings.Default.FilesystemTabIsVisible;
            m_StreamsTabIsVisible.IsChecked = Settings.Default.StreamsTabIsVisible;
            m_PlaylistsTabIsVisible.IsChecked = Settings.Default.PlaylistsTabIsVisible;
            SelectDefaultMusicCollectionTab(Settings.Default.DefaultMusicCollectionTab);
            ServerList.Deserialize(Settings.Default.Servers);
            OnServerListChanged(null, null);

            m_SendToPlaylistMethodAddAsNext.IsChecked = Settings.Default.SendToPlaylistMethod == SendToPlaylistMethod.AddAsNext.ToString();
            m_SendToPlaylistMethodReplaceAndPlay.IsChecked = Settings.Default.SendToPlaylistMethod == SendToPlaylistMethod.ReplaceAndPlay.ToString();
            m_SendToPlaylistMethodAppend.IsChecked = !m_SendToPlaylistMethodAddAsNext.IsChecked.Value && !m_SendToPlaylistMethodReplaceAndPlay.IsChecked.Value;

            string formats = "";

            foreach (string s in Settings.Default.AlbumDateFormats)
            {
                if (formats.Length > 0)
                {
                    formats += ";";
                }

                formats += s;
            }

            m_DateFormatsEntry.Text = formats;
        }

        private void SaveSettings()
        {
            AlbumSortingMode albumSortingMode = m_SortAlbumsByDate.IsChecked.Value ? AlbumSortingMode.ByDate : AlbumSortingMode.ByName;
            string servers = ServerList.Serialize();
            bool reconnectNeeded =
                m_UseAlbumArtist.IsChecked != Settings.Default.UseAlbumArtist ||
                albumSortingMode.ToString() != Settings.Default.AlbumSortingMode ||
                m_DateFormatsEntry.Text != StringCollectionAsString(Settings.Default.AlbumDateFormats) ||
                servers != Settings.Default.Servers;

            Settings.Default.Servers = servers;
            Settings.Default.ViewUpdateInterval = Utils.StringToInt(m_UpdateIntervalEntry.Text, 500);
            Settings.Default.NetworkTimeout = Utils.StringToInt(m_NetworkTimeoutEntry.Text, 10);
            Settings.Default.ReconnectInterval = Utils.StringToInt(m_ReconnectIntervalEntry.Text, 10);
            Settings.Default.VolumeAdjustmentStep = Utils.StringToInt(m_WheelVolumeStepEntry.Text, 5);
            Settings.Default.MouseWheelAdjustsSongPositionInPercent = m_WheelSongPositioningModeIsPercent.IsChecked.Value;
            Settings.Default.MouseWheelAdjustsSongPositionPercentBy = Utils.StringToInt(m_WheelSongPositioningPercentEntry.Text, 5);
            Settings.Default.MouseWheelAdjustsSongPositionSecondsBy = Utils.StringToInt(m_WheelSongPositioningSecondsEntry.Text, 5);
            Settings.Default.EnableVolumeControl = m_EnableVolumeControl.IsChecked == true;
            Settings.Default.UseAlbumArtist = m_UseAlbumArtist.IsChecked == true;
            Settings.Default.AlbumSortingMode = albumSortingMode.ToString();
            Settings.Default.AlbumDateFormats = StringAsStringCollection(m_DateFormatsEntry.Text);
            Settings.Default.QuickSearchTabIsVisible = m_QuickSearchTabIsVisible.IsChecked == true;
            Settings.Default.ArtistListTabIsVisible = m_ArtistListTabIsVisible.IsChecked == true;
            Settings.Default.ArtistTreeTabIsVisible = m_ArtistTreeTabIsVisible.IsChecked == true;
            Settings.Default.GenreListTabIsVisible = m_GenreListTabIsVisible.IsChecked == true;
            Settings.Default.GenreTreeTabIsVisible = m_GenreTreeTabIsVisible.IsChecked == true;
            Settings.Default.FilesystemTabIsVisible = m_FilesystemTabIsVisible.IsChecked == true;
            Settings.Default.AdvancedTabIsVisible = m_AdvancedSearchTabIsVisible.IsChecked == true;
            Settings.Default.StreamsTabIsVisible = m_StreamsTabIsVisible.IsChecked == true;
            Settings.Default.PlaylistsTabIsVisible = m_PlaylistsTabIsVisible.IsChecked == true;
            Settings.Default.DefaultMusicCollectionTab = SelectedDefaultMusicCollectionTab().ToString();

            if (m_SendToPlaylistMethodAddAsNext.IsChecked == true)
            {
                Settings.Default.SendToPlaylistMethod = SendToPlaylistMethod.AddAsNext.ToString();
            }
            else if (m_SendToPlaylistMethodReplaceAndPlay.IsChecked == true)
            {
                Settings.Default.SendToPlaylistMethod = SendToPlaylistMethod.ReplaceAndPlay.ToString();
            }
            else
            {
                Settings.Default.SendToPlaylistMethod = SendToPlaylistMethod.Append.ToString();
            }

            Settings.Default.InitialSetupDone = true;
            Settings.Default.Save();

            m_DataModel.MainWindow.SettingsChanged(reconnectNeeded);
        }

        private string StringCollectionAsString(StringCollection strings)
        {
            string result = "";

            foreach (string str in strings)
            {
                if (result.Length > 0)
                {
                    result += m_StringCollectionSeparator;
                }

                result += str;
            }

            return result;
        }

        private StringCollection StringAsStringCollection(string mergedString)
        {
            StringCollection result = new StringCollection();
            string[] parts = mergedString.Split(m_StringCollectionSeparator);

            foreach (string part in parts)
            {
                result.Add(part);
            }

            return result;
        }

        private void TabPreferencesSanityCheck(object sender, RoutedEventArgs e)
        {
            if (m_QuickSearchTabIsDefault.IsChecked.HasValue && m_QuickSearchTabIsDefault.IsChecked.Value)
                m_QuickSearchTabIsVisible.IsChecked = true;
            else if (m_AdvancedSearchTabIsDefault.IsChecked.HasValue && m_AdvancedSearchTabIsDefault.IsChecked.Value)
                m_AdvancedSearchTabIsVisible.IsChecked = true;
            else if (m_ArtistListTabIsDefault.IsChecked.HasValue && m_ArtistListTabIsDefault.IsChecked.Value)
                m_ArtistListTabIsVisible.IsChecked = true;
            else if (m_ArtistTreeTabIsDefault.IsChecked.HasValue && m_ArtistTreeTabIsDefault.IsChecked.Value)
                m_ArtistTreeTabIsVisible.IsChecked = true;
            else if (m_GenreListTabIsDefault.IsChecked.HasValue && m_GenreListTabIsDefault.IsChecked.Value)
                m_GenreListTabIsVisible.IsChecked = true;
            else if (m_GenreTreeTabIsDefault.IsChecked.HasValue && m_GenreTreeTabIsDefault.IsChecked.Value)
                m_GenreTreeTabIsVisible.IsChecked = true;
            else if (m_FilesystemTabIsDefault.IsChecked.HasValue && m_FilesystemTabIsDefault.IsChecked.Value)
                m_FilesystemTabIsVisible.IsChecked = true;
            else if (m_StreamsTabIsDefault.IsChecked.HasValue && m_StreamsTabIsDefault.IsChecked.Value)
                m_StreamsTabIsVisible.IsChecked = true;
            else if (m_PlaylistsTabIsDefault.IsChecked.HasValue && m_PlaylistsTabIsDefault.IsChecked.Value)
                m_PlaylistsTabIsVisible.IsChecked = true;
        }

        private void OnClose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            m_DataModel.MainWindow.OnChildWindowClosing(this);
        }

        private MusicCollectionTab SelectedDefaultMusicCollectionTab()
        {
            if (m_QuickSearchTabIsDefault.IsChecked.HasValue && m_QuickSearchTabIsDefault.IsChecked.Value)
                return MusicCollectionTab.QuickSearchTab;
            else if (m_AdvancedSearchTabIsDefault.IsChecked.HasValue && m_AdvancedSearchTabIsDefault.IsChecked.Value)
                return MusicCollectionTab.AdvancedSearchTab;
            else if (m_ArtistListTabIsDefault.IsChecked.HasValue && m_ArtistListTabIsDefault.IsChecked.Value)
                return MusicCollectionTab.ArtistListTab;
            else if (m_ArtistTreeTabIsDefault.IsChecked.HasValue && m_ArtistTreeTabIsDefault.IsChecked.Value)
                return MusicCollectionTab.ArtistTreeTab;
            else if (m_GenreListTabIsDefault.IsChecked.HasValue && m_GenreListTabIsDefault.IsChecked.Value)
                return MusicCollectionTab.GenreListTab;
            else if (m_GenreTreeTabIsDefault.IsChecked.HasValue && m_GenreTreeTabIsDefault.IsChecked.Value)
                return MusicCollectionTab.GenreTreeTab;
            else if (m_FilesystemTabIsDefault.IsChecked.HasValue && m_FilesystemTabIsDefault.IsChecked.Value)
                return MusicCollectionTab.FilesystemTab;
            else if (m_StreamsTabIsDefault.IsChecked.HasValue && m_StreamsTabIsDefault.IsChecked.Value)
                return MusicCollectionTab.StreamsTab;
            else if (m_PlaylistsTabIsDefault.IsChecked.HasValue && m_PlaylistsTabIsDefault.IsChecked.Value)
                return MusicCollectionTab.PlaylistsTab;

            return MusicCollectionTab.QuickSearchTab;
        }

        private void SelectDefaultMusicCollectionTab(string tab)
        {
            m_QuickSearchTabIsDefault.IsChecked = tab == MusicCollectionTab.QuickSearchTab.ToString();
            m_AdvancedSearchTabIsDefault.IsChecked = tab == MusicCollectionTab.AdvancedSearchTab.ToString();
            m_ArtistListTabIsDefault.IsChecked = tab == MusicCollectionTab.ArtistListTab.ToString();
            m_ArtistTreeTabIsDefault.IsChecked = tab == MusicCollectionTab.ArtistTreeTab.ToString();
            m_GenreListTabIsDefault.IsChecked = tab == MusicCollectionTab.GenreListTab.ToString();
            m_GenreTreeTabIsDefault.IsChecked = tab == MusicCollectionTab.GenreTreeTab.ToString();
            m_FilesystemTabIsDefault.IsChecked = tab == MusicCollectionTab.FilesystemTab.ToString();
            m_StreamsTabIsDefault.IsChecked = tab == MusicCollectionTab.StreamsTab.ToString();
            m_PlaylistsTabIsDefault.IsChecked = tab == MusicCollectionTab.PlaylistsTab.ToString();
        }

        private void OnAddServerClicked(object sender, RoutedEventArgs e)
        {
            ServerList.Add("localhost", 6600, "");
            m_ServerSettings.ScrollIntoView(ServerList.SelectedServer);
        }

        private void OnDeleteServerClicked(object sender, RoutedEventArgs e)
        {
            ServerList.RemoveSelected();
        }

        public ServerList ServerList
        {
            get;
            private set;
        }

        
        private void OnServerListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OnServerListChanged(null, null);
        }
        

        private void OnServerListChanged(object sender, PropertyChangedEventArgs e)
        {
            ServerEntry server = ServerList.SelectedServer;

            if (server != null)
            {
                m_HostnameEntry.Text = server.Hostname;
                m_PortEntry.Text = server.Port.ToString();
                m_PasswordEntry.Password = Crypto.DecryptPassword(server.EncryptedPassword);
            }
        }

        private void ServerEntryUpdated(object sender, RoutedEventArgs e)
        {
            ValidateOptions();

            if (m_ServerSettings.SelectedIndex != -1)
            {
                ServerList.SelectedServer.Hostname = m_HostnameEntry.Text;
                ServerList.SelectedServer.Port = Utils.StringToInt(m_PortEntry.Text) ?? 6600;
                ServerList.SelectedServer.EncryptedPassword = Crypto.EncryptPassword(m_PasswordEntry.Password);
            }
        }
    }
}
