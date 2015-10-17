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

using Auremo.MusicLibrary;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Auremo
{
    public class PlaylistWriter
    {
        public static string Write(IEnumerable<LibraryItem> items)
        {
            if (items.Count() > 0)
            {
                StringBuilder result = new StringBuilder();

                result.Append("[playlist]\r\n");
                result.Append("NumberOfEntries=" + items.Count() + "\r\n");

                int entryIndex = 1;

                foreach (AudioStream entry in items)
                {
                    result.Append("File" + entryIndex + "=" + entry.Path + "\r\n");
                    result.Append("Title" + entryIndex + "=" + entry.Label + "\r\n");
                    entryIndex += 1;
                }

                result.Append("Version=2\r\n");

                return result.ToString();
            }
            else
            {
                return null;
            }
        }
    }
}
