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
    public class GenreFilteredAlbum : LibraryItem
    {
        public GenreFilteredAlbum(Genre genre, Artist artist, string title, string date, string directory)
        {
            Genre = genre;
            Artist = artist;
            Title = title;
            Date = date;
        }

        public Genre Genre
        {
            get;
            set;
        }

        public Artist Artist
        {
            get;
            set;
        }

        public string Title
        {
            get;
            set;
        }

        public string Date
        {
            get;
            set;
        }

        public string Directory
        {
            get;
            private set;
        }

        public string Year
        {
            get
            {
                return Utils.ExtractYearFromDateString(Date);
            }
        }

        public override string DisplayString
        {
            get
            {
                return Artist + ": " + Title;
            }
        }

        public override int CompareTo(object o)
        {
            if (o is GenreFilteredAlbum)
            {
                GenreFilteredAlbum rhs = o as GenreFilteredAlbum;
                int result = Genre.CompareTo(rhs.Genre);

                if (result == 0)
                {
                    result = Artist.CompareTo(rhs.Artist);

                    if (result == 0)
                    {
                        result = StringComparer.Ordinal.Compare(Title, rhs.Title);
                    }
                }

                return result;
            }
            else
            {
                throw new Exception("GenreFilteredAlbum: attempt to compare to an incompatible object");
            }
        }

        public override string ToString()
        {
            return DisplayString;
        }
    }
}
