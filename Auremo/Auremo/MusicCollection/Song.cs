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

namespace Auremo.MusicCollection
{
    public class Song : MusicCollectionItem
    {
        private string m_Path = null;
        private string m_PathTypePrefix = null; // This appears to be Mopidy-specific.

        public Song(string path)
        {
            Path = path;

            Artist = null;
            Album = null;
            Genre = null;
            Length = null;
            Track = null;
            Date = null;
            Directory = "";
            Filename = "";
        }

        public string Path
        {
            get
            {
                return m_Path;
            }
            set
            {
                m_Path = value;
                string strippedPath = value;

                if (strippedPath.StartsWith("local:track:"))
                {
                    m_PathTypePrefix = "local:track:";
                    strippedPath = strippedPath.Substring(12);
                }
                else if (strippedPath.StartsWith("spotify:track:"))
                {
                    m_PathTypePrefix = "spotify:track:";
                    strippedPath = strippedPath.Substring(14);
                }

                int lastSlash = strippedPath.LastIndexOf('/');

                if (lastSlash >= 0)
                {
                    Directory = strippedPath.Substring(0, lastSlash);
                    strippedPath = strippedPath.Substring(lastSlash + 1);
                }

                Filename = strippedPath;
            }
        }

        public string Directory
        {
            get;
            private set;
        }

        public string Filename
        {
            get;
            private set;
        }

        public string Title
        {
            get;
            set;
        }

        public string Artist
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

        public override string DisplayName
        {
            get
            {
                return Artist + ": " + Title + " (" + Album + ")";
            }
        }

        public override int CompareTo(object o)
        {
            if (o is Song)
            {
                Song rhs = (Song)o;
                return StringComparer.Ordinal.Compare(Path, rhs.Path);
            }
            else if (o is StreamMetadata)
            {
                return -1;
            }
            else
            {
                throw new Exception("Song: attempt to compare to an incompatible object");
            }
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
