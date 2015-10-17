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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace Auremo.GUI
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Return the selected items in the container in top-down order.
        /// </summary>
        public static IList<LibraryItem> Selection(this DataGrid container)
        {
            return container.SelectedItems.Cast<IndexedLibraryItem>().OrderBy(e => e.Position).Select(e => e.Item).ToList();
        }

        /// <summary>
        /// Return the selected items, asserting that they are T, in the container in top-down order.
        /// </summary>
        public static IList<T> Selection<T>(this DataGrid container) where T : LibraryItem
        {
            return container.SelectedItems.Cast<IndexedLibraryItem>().OrderBy(e => e.Position).Select(e => e.Item as T).ToList();
        }
    }
}
