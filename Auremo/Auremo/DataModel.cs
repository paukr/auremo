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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Auremo.Properties;

namespace Auremo
{
    public class DataModel
    {
        public DataModel(MainWindow mainWindow)
        {
            MainWindow = mainWindow;
            ServerList = new ServerList();
            ServerSession = new ServerSession(this);
            ServerStatus = new ServerStatus(this);
            Database = new Database(this);
            QuickSearch = new QuickSearch(this);
            AdvancedSearch = new AdvancedSearch(this);
            DatabaseView = new DatabaseView(this);
            StreamsCollection = new StreamsCollection();
            SavedPlaylists = new SavedPlaylists(this);
            CurrentSong = new CurrentSong(this);
            Playlist = new Playlist(this);
            OutputCollection = new OutputCollection(this);
            CustomDateNormalizer = new DateNormalizer();
            CustomDateNormalizer.ReadFromSettings();
            YearNormalizer = new DateNormalizer(new string[] {"YYYY"});
        }

        public MainWindow MainWindow
        {
            get;
            private set;
        }

        public ServerList ServerList
        {
            get;
            private set;
        }

        public ServerSession ServerSession
        {
            get;
            private set;
        }

        public ServerStatus ServerStatus
        {
            get;
            private set;
        }

        public Database Database
        {
            get;
            private set;
        }

        public QuickSearch QuickSearch
        {
            get;
            private set;
        }

        public AdvancedSearch AdvancedSearch
        {
            get;
            private set;
        }

        public DatabaseView DatabaseView
        {
            get;
            private set;
        }

        public StreamsCollection StreamsCollection
        {
            get;
            private set;
        }

        public SavedPlaylists SavedPlaylists
        {
            get;
            private set;
        }

        public Playlist Playlist
        {
            get;
            private set;
        }

        public CurrentSong CurrentSong
        {
            get;
            private set;
        }
        
        public OutputCollection OutputCollection
        {
            get;
            private set;
        }

        public DateNormalizer CustomDateNormalizer
        {
            get;
            private set;
        }

        public DateNormalizer YearNormalizer
        {
            get;
            private set;
        }
    }
}
