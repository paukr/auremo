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
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Auremo.MusicLibrary
{
    /// <summary>
    /// A wrapper for LibraryItem that adds indexing and selection features
    /// useful for use in DataGrids.
    /// </summary>
    public class IndexedLibraryItem : INotifyPropertyChanged, IComparable
    {
        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        #endregion

        private bool m_IsSelected = false;

        public IndexedLibraryItem(LibraryItem item, int position)
        {
            Item = item;
            Position = position;
        }

        public IndexedLibraryItem(LibraryItem item, int position, bool isSelected)
        {
            Item = item;
            Position = position;
            IsSelected = isSelected;
        }

        public LibraryItem Item
        {
            get;
            private set;
        }

        public T ItemAs<T>() where T : LibraryItem
        {
            if (!(Item is T))
            {
                throw new Exception("IndexedLibraryItem: improper cast in call to ItemAs<T>().");
            }

            return (T)Item;
        }

        public int Position
        {
            get;
            private set;
        }

        public bool IsSelected
        {
            get
            {
                return m_IsSelected;
            }
            set
            {
                if (value != m_IsSelected)
                {
                    m_IsSelected = value;
                    NotifyPropertyChanged("IsSelected");
                }
            }
        }

        public int CompareTo(object o)
        {
            if (o is IndexedLibraryItem)
            {
                return Position - ((IndexedLibraryItem)o).Position;
            }
            else
            {
                throw new Exception("MusicCollectionListItem: attempt to compare to an incompatible object");
            }
        }

        public override string ToString()
        {
            return Item.ToString();
        }
    }
}
