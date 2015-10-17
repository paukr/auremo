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

using Auremo.Properties;
using Auremo.MusicLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Auremo
{
    public class MPDSongResponseBlock
    {
        public MPDSongResponseBlock(string file)
        {
            File = file;
            Album = null;
            Artist = null;
            AlbumArtist = null;
            Date = null;
            Genre = null;
            Id = -1;
            Name = null;
            Pos = -1; 
            Time = null;
            Title = null;
            Track = -1;
        }

        public string File
        {
            get;
            set;
        }

        public string Album
        {
            get;
            set;
        }

        public string Artist
        {
            get;
            set;
        }

        public string AlbumArtist
        {
            get;
            set;
        }

        public string Date
        {
            get;
            set;
        }

        public string Genre
        {
            get;
            set;
        }

        public int Id
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public int Pos
        {
            get;
            set;
        }

        public int? Time
        {
            get;
            set;
        }

        public string Title
        {
            get;
            set;
        }

        public int Track
        {
            get;
            set;
        }

        public override string ToString()
        {
            return Artist + ": " + Title + " (" + Album + ")";   
        }
    }
}
