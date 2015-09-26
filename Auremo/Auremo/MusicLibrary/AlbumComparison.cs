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

using Auremo.MusicLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Auremo.MusicLibrary
{
    public class AlbumByDateComparer : IComparer<Album>
    {
        public int Compare(Album lhs, Album rhs)
        {
            int artistComparison = lhs.Artist.CompareTo(rhs.Artist);

            if (artistComparison != 0)
            {
                return artistComparison;
            }
            else if (lhs.Date == rhs.Date)
            {
                return lhs.Title.CompareTo(rhs.Title);
            }
            else if (lhs.Date == null)
            {
                return 1;
            }
            else if (rhs.Date == null)
            {
                return -1;
            }
            else
            {
                return lhs.Date.CompareTo(rhs.Date);
            }
        }
    }

    public class AlbumByTitleComparer : IComparer<Album>
    {
        public int Compare(Album lhs, Album rhs)
        {
            int artistComparison = lhs.Artist.CompareTo(rhs.Artist);

            if (artistComparison != 0)
            {
                return artistComparison;
            }
            else
            {
                return lhs.Title.CompareTo(rhs.Title);
            }
        }
    }
}
