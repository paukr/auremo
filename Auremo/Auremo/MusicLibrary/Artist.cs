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
    public class Artist : LibraryItem
    {
        public static readonly string Unknown = "Unknown Artist";
        public Artist(string name)
        {
            Name = name;
        }

        public string Name
        {
            get;
            private set;
        }

        public override string DisplayString
        {
            get
            {
                return Name;
            }
        }

        public override int CompareTo(object o)
        {
            if (o is Artist)
            {
                return StringComparer.Ordinal.Compare(Name, (o as Artist).Name);
            }
            else
            {
                throw new Exception("Artist: attempt to compare to an incompatible object");
            }
        }

        public override string ToString()
        {
            return DisplayString;
        }
    }
}
