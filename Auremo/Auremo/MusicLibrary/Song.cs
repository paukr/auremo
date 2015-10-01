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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Auremo.MusicLibrary
{
    /// <summary>
    /// A song or track that is a part of the local music database.
    /// Singleton copies of all songs stored in the Database object in the DataModel.
    /// </summary>
    public class Song : LibraryItem, Playable
    {
        public Song(MPDSongResponseBlock block)
        {
            Path = new Path(block.File);
            Title = block.Title;
            Length = block.Time;
            Track = block.Track;
            Length = block.Time;
            Filename = Path.Directories.Last();

            // These need to be set by the caller as they require external external objects.
            Artist = null;
            Album = null;
            Genre = null;
            Date = null;
            Directory = null;
        }

        public Path Path
        {
            get;
            private set;
        }

        public string Title
        {
            get;
            private set;
        }

        public Directory Directory
        {
            get;
            set;
        }

        public string Filename
        {
            get;
            private set;
        }

        public Artist Artist
        {
            get;
            set;
        }

        public Album Album
        {
            get;
            set;
        }

        public Genre Genre
        {
            get;
            set;
        }

        public GenreFilteredAlbum GenreFilteredAlbum
        {
            get;
            set;
        }

        public int? Length
        {
            get;
            private set;
        }

        public int? Track
        {
            get;
            private set;
        }

        public string Date
        {
            get;
            set;
        }

        public string Year
        {
            get
            {
                return Utils.ExtractYearFromDateString(Date);
            }
        }

        public bool IsLocal
        {
            get
            {
                return Path.IsLocal();
            }
        }

        public bool IsSpotify
        {
            get
            {
                return Path.IsSpotify();
            }
        }

        public override string DisplayString
        {
            get
            {
                return Title ?? Filename;
            }
        }

        public string FilesystemDisplayString
        {
            get
            {
                return Filename;
            }
        }

        public override int CompareTo(object o)
        {
            if (o is Song)
            {
                Song rhs = (Song)o;
                return StringComparer.Ordinal.Compare(Path, rhs.Path);
            }
            else
            {
                throw new Exception("Song: attempt to compare to an incompatible object");
            }
        }

        public override string ToString()
        {
            return DisplayString;
        }
    }
}
