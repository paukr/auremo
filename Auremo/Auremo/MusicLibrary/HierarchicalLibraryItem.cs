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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Auremo.MusicLibrary
{
    /// <summary>
    /// A wrapper for LibraryItem that adds child items and selection features
    /// useful for use in TreeViews.
    /// </summary>
    public class HierarchicalLibraryItem : INotifyPropertyChanged, IComparable
    {
        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        #endregion

        private bool m_IsSelected = false;
        private bool m_IsExpanded = false;
        private bool m_IsMultiSelected = false;

        /// <summary>
        /// Create a root node.
        /// </summary>
        public HierarchicalLibraryItem(LibraryItem item, HierarchyController controller)
        {
            Item = item;
            Parent = null;
            Children = new ObservableCollection<HierarchicalLibraryItem>();
            Controller = controller;
            Id = -1;
            HighestChildId = -1;
            Controller.RootLevelNodes.Add(this);
        }

        /// <summary>
        /// Create a child node.
        /// </summary>
        public HierarchicalLibraryItem(LibraryItem item, HierarchicalLibraryItem parent)
        {
            Item = item;
            Parent = parent;
            Parent.Children.Add(this);
            Children = new ObservableCollection<HierarchicalLibraryItem>();
            Controller = Parent.Controller;
            Id = -1;
            HighestChildId = -1;
        }
        
        public LibraryItem Item
        {
            get;
            private set;
        }

        public HierarchicalLibraryItem Parent
        {
            get;
            private set;
        }

        public HierarchicalLibraryItem Root
        {
            get
            {
                return Parent == null ? this : Parent.Root;
            }
        }

        public ObservableCollection<HierarchicalLibraryItem> Children
        {
            get;
            private set;
        }

        /// <summary>
        /// Return the LibraryItems contained in leaf-level children.
        /// </summary>
        public IEnumerable<LibraryItem> LeafItems
        {
            get
            {
                IList<LibraryItem> result = new List<LibraryItem>();
                FillLeafItemsRecursively(result);
                return result;
            }
        }

        private void FillLeafItemsRecursively(IList<LibraryItem> result)
        {
            if (Children.Count == 0)
            {
                result.Add(Item);
            }
            else
            {
                foreach (HierarchicalLibraryItem child in Children)
                {
                    child.FillLeafItemsRecursively(result);
                }
            }
        }

        public void AddChild(HierarchicalLibraryItem child)
        {
            Children.Add(child);
            NotifyPropertyChanged("Children");
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

        public bool IsExpanded
        {
            get
            {
                return m_IsExpanded;
            }
            set
            {
                if (value != m_IsExpanded)
                {
                    m_IsExpanded = value;

                    if (m_IsExpanded)
                    {
                        if (Parent != null)
                        {
                            Parent.IsExpanded = true;
                        }
                    }
                    else
                    {
                        foreach (HierarchicalLibraryItem child in Children)
                        {
                            child.IsExpanded = false;
                        }
                    }

                    NotifyPropertyChanged("IsExpanded");
                }
            }
        }

        public bool IsMultiSelected
        {
            get
            {
                return m_IsMultiSelected;
            }
            set
            {
                if (value != m_IsMultiSelected)
                {
                    if (value)
                    {
                        Controller.MultiSelection.Add(this);

                        if (Parent != null)
                        {
                            Parent.IsExpanded = true;
                        }
                    }
                    else
                    {
                        Controller.MultiSelection.Remove(this);
                    }

                    m_IsMultiSelected = value;
                    NotifyPropertyChanged("IsMultiSelected");
                }
            }
        }

        public HierarchyController Controller
        {
            get;
            private set;
        }

        public int Id
        {
            get;
            set;
        }

        public int HighestChildId
        {
            get;
            set;
        }

        public string DisplayString
        {
            get
            {
                return Item.DisplayString;
            }
        }

        public int CompareTo(object o)
        {
            if (o is HierarchicalLibraryItem)
            {
                return Id - (o as HierarchicalLibraryItem).Id;
            }
            else
            {
                throw new Exception("HierarchicalLibraryItem: attempt to compare to an incompatible object");
            }
        }

        public override string ToString()
        {
            return DisplayString;
        }
    }
}
