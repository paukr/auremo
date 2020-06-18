/*
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

using System;
using System.Collections.Generic;
using System.Linq;

namespace Auremo.MusicLibrary
{
    public class Directory : LibraryItem
    {
        public Directory(string name, Directory parent)
        {
            Name = name;
            Parent = parent;
            Children = new List<LibraryItem>();
        }

        public string Name
        {
            get;
            private set;
        }
        
        public Directory Parent
        {
            get;
            private set;
        }

        public ICollection<LibraryItem> Children
        {
            get;
            private set;
        }

        public string Full
        {
            get
            {
                return Parent == null ? Name : Parent.Full + "/" + Name;
            }
        }

        public override string DisplayString
        {
            get
            {
                return Name;
            }
        }

        public string FilesystemDisplayString
        {
            get
            {
                var date = Children.OfType<Song>().Select(x => x.LastModified).DefaultIfEmpty().Max();
                return $"{Name} {(date != DateTime.MinValue ? $"[{date.ToLocalTime()}]" : "")}";
            }
        }

        public override int CompareTo(object o)
        {
            if (o is Directory)
            {
                return StringComparer.Ordinal.Compare(Full, (o as Directory).Full);
            }
            else
            {
                throw new Exception("Directory: attempt to compare to an incompatible object");
            }
        }

        public override string ToString()
        {
            return DisplayString;
        }
    }
}
