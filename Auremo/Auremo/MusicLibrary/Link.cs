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

namespace Auremo.MusicLibrary
{
    /// <summary>
    /// An unspecified playable captured from a non-database MPD response. In
    /// Mopidy this includes links to artists, albums etc that can be expanded
    /// by sending them to the playlist, but not by the Database object.
    /// </summary>
    public class Link : LibraryItem, Playable
    {
        public Link(Path path)
        {
            Path = path;
            Title = null;
            Artist = null;
            Album = null;
            Date = null;
        }

        public Path Path
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

        public string Album
        {
            get;
            set;
        }

        public string Date
        {
            get;
            set;
        }

        public override string DisplayString
        {
            get
            {
                return Title ?? Path.Full;
            }
        }

        public override int CompareTo(object o)
        {
            if (o is Link)
            {
                return StringComparer.Ordinal.Compare(Path, (o as Link).Path);
            }
            else
            {
                throw new Exception("Link: attempt to compare to an incompatible object");
            }
        }

        public override string ToString()
        {
            return DisplayString;
        }
    }
}
