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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Auremo.MusicLibrary
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Clear previous contents and insert all items from source to the ObservableCollection, wrapped into MusicCollectionListItems.
        /// </summary>
        public static void CreateFrom(this ObservableCollection<IndexedLibraryItem> target, IEnumerable<LibraryItem> source)
        {
            target.Clear();
            target.AddFrom(source);
        }

        /// <summary>
        /// Insert all items from source to the ObservableCollection, wrapped into MusicCollectionListItems.
        /// </summary>
        public static void AddFrom(this ObservableCollection<IndexedLibraryItem> target, IEnumerable<LibraryItem> source)
        {
            foreach (LibraryItem item in source)
            {
                target.Add(new IndexedLibraryItem(item, target.Count));
            }
        }

        /// <summary>
        /// Return the content items of all selected rows.
        /// </summary>
        public static IList<LibraryItem> SelectedItems(this IEnumerable<IndexedLibraryItem> source)
        {
            IList<LibraryItem> result = new List<LibraryItem>();

            foreach (IndexedLibraryItem el in source)
            {
                if (el.IsSelected)
                {
                    result.Add(el.Item);
                }
            }

            return result;
        }

        /// <summary>
        /// Return the content items, asserting that they are all T, of all selected rows.
        /// </summary>
        public static IList<T> SelectedItems<T>(this IEnumerable<IndexedLibraryItem> source) where T : LibraryItem
        {
            IList<T> result = new List<T>();

            foreach (IndexedLibraryItem el in source)
            {
                if (el.IsSelected)
                {
                    result.Add(el.Item as T);
                }
            }

            return result;
        }
        
        /// <summary>
        /// Append list rhs to this.
        /// </summary>
        public static void AddAll<T>(this IList<T> lhs, IEnumerable<T> rhs)
        {
            foreach (T e in rhs)
            {
                lhs.Add(e);
            }
        }
    }
}
