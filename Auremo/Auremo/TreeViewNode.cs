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
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Auremo
{
    public abstract class TreeViewNode : INotifyPropertyChanged, IComparable
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

        protected TreeViewNode(TreeViewNode parent, TreeViewController controller)
        {
            Parent = parent;
            Children = new ObservableCollection<TreeViewNode>();
            Controller = controller;
            ID = -1;
            HighestChildID = -1;
        }

        public TreeViewNode Root
        {
            get
            {
                TreeViewNode root = this;

                while (root.Parent != null)
                {
                    root = root.Parent;
                }

                return root;
            }
        }

        public TreeViewNode Parent
        {
            get;
            private set;
        }

        public IList<TreeViewNode> Children
        {
            get;
            private set;
        }

        public virtual void AddChild(TreeViewNode child)
        {
            // Virtual so that leaf types can forbid this.
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
                        foreach (TreeViewNode child in Children)
                        {
                            child.OnAncestorCollapsed();
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

        public TreeViewController Controller
        {
            get;
            private set;
        }

        public int ID
        {
            get;
            set;
        }

        public int HighestChildID
        {
            get;
            set;
        }

        public void OnAncestorCollapsed()
        {
            IsMultiSelected = false;

            foreach (TreeViewNode child in Children)
            {
                child.OnAncestorCollapsed();
            }
        }

        public int CompareTo(object o)
        {
            if (o is TreeViewNode)
            {
                return ID - ((TreeViewNode)o).ID;
            }
            else
            {
                throw new Exception("TreeViewNode: attempt to compare to an incompatible object");
            }
        }

        public abstract string DisplayString
        {
            get;
        }
    }
}
