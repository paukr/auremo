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

namespace Auremo.MusicLibrary
{
    /// <summary>
    /// A song or track that is a part of the local music database.
    /// Singleton copies of all songs stored in the Database object in the DataModel.
    /// </summary>
    public class Song : LibraryItem, Playable
    {
        private string m_PathTypePrefix = null; // This appears to be Mopidy-specific.

        public Song(Path path)
        {
            Path = path;

            Artist = null;
            Album = null;
            Genre = null;
            Length = null;
            Track = null;
            Date = null;
            Directory = null;
            Filename = Path.Directories.Last();
        }

        public Path Path
        {
            get;
            set;
        }

        public string Title
        {
            get;
            set;
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
            set;
        }

        public int? Track
        {
            get;
            set;
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
                return m_PathTypePrefix == null || m_PathTypePrefix == "local:track:";
            }
        }

        public bool IsSpotify
        {
            get
            {
                return m_PathTypePrefix == "spotify:track:";
            }
        }

        public override string DisplayString
        {
            get
            {
                return Title;
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
