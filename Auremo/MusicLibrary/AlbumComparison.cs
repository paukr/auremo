﻿/*
 * Copyright 2016 Mikko Teräs and Niilo Säämänen.
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

using System.Collections.Generic;

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

    public class AlbumByDirectoryComparer : IComparer<Album>
    {
        public int Compare(Album lhs, Album rhs)
        {
            int directoryComparison = lhs.Directory.CompareTo(rhs.Directory);

            if (directoryComparison != 0)
            {
                return directoryComparison;
            }

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

    public class GenreFilteredAlbumByDateComparer : IComparer<GenreFilteredAlbum>
    {
        public int Compare(GenreFilteredAlbum lhs, GenreFilteredAlbum rhs)
        {
            int genreComparison = lhs.Genre.CompareTo(rhs.Genre);

            if (genreComparison != 0)
            {
                return genreComparison;
            }
            else if (lhs.Artist != rhs.Artist)
            {
                return lhs.Artist.CompareTo(rhs.Artist);
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

    public class GenreFilteredAlbumByDirectoryComparer : IComparer<GenreFilteredAlbum>
    {
        public int Compare(GenreFilteredAlbum lhs, GenreFilteredAlbum rhs)
        {
            int genreComparison = lhs.Genre.CompareTo(rhs.Genre);

            if (genreComparison != 0)
            {
                return genreComparison;
            }

            int directoryComparison = lhs.Directory.CompareTo(rhs.Directory);

            if (directoryComparison != 0)
            {
                return directoryComparison;
            }
            else
            {
                return lhs.Title.CompareTo(rhs.Title);
            }
        }
    }

    public class GenreFilteredAlbumByTitleComparer : IComparer<GenreFilteredAlbum>
    {
        public int Compare(GenreFilteredAlbum lhs, GenreFilteredAlbum rhs)
        {
            int genreComparison = lhs.Genre.CompareTo(rhs.Genre);

            if (genreComparison != 0)
            {
                return genreComparison;
            }
            else
            {
                return lhs.Title.CompareTo(rhs.Title);
            }
        }
    }
}
