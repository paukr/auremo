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

namespace Auremo
{
    public class TreeViewController
    {
        private IList<TreeViewNode> m_RootLevelNodes = null;

        public TreeViewController(IList<TreeViewNode> rootLevelNodes)
        {
            m_RootLevelNodes = rootLevelNodes;
            MultiSelection = new ObservableCollection<TreeViewNode>();
        }

        public IEnumerable<TreeViewNode> RootLevelNodes
        {
            get
            {
                return m_RootLevelNodes;
            }
        }

        public TreeViewNode FirstNode
        {
            get
            {
                if (m_RootLevelNodes == null || m_RootLevelNodes.Count == 0)
                {
                    return null;
                }
                else
                {
                    return m_RootLevelNodes.First();
                }
            }
        }

        public void ClearMultiSelection()
        {
            while (MultiSelection.Count > 0)
            {
                MultiSelection.First().IsMultiSelected = false;
            }
        }

        public void SelectRange(TreeViewNode toNode)
        {
            if (Pivot != null)
            {
                int minID = Math.Min(Pivot.ID, toNode.ID);
                int maxID = Math.Max(Pivot.ID, toNode.ID);

                foreach (TreeViewNode root in m_RootLevelNodes)
                {
                    SelectVisibleWithinRange(root, minID, maxID);
                }
            }
        }

        private void SelectVisibleWithinRange(TreeViewNode node, int minID, int maxID)
        {
            // TODO: optimize more.
            if (minID <= node.ID && node.ID <= maxID)
            {
                node.IsMultiSelected = true;
            }

            if (node.IsExpanded && node.ID <= maxID && node.HighestChildID >= minID)
            {
                foreach (TreeViewNode child in node.Children)
                {
                    SelectVisibleWithinRange(child, minID, maxID);

                    if (child.ID > maxID)
                    {
                        return;
                    }
                }
            }
        }

        // Start point of range selection (mouse or key with shift down).
        public TreeViewNode Pivot
        {
            get;
            set;
        }

        public TreeViewNode Current
        {
            get;
            set;
        }

        public TreeViewNode Previous
        {
            get
            {
                if (Current == null)
                {
                    return null;
                }
                else
                {
                    return GetPredecessor(Current, m_RootLevelNodes, m_RootLevelNodes.First());
                }
            }
        }

        public TreeViewNode Next
        {
            get
            {
                if (Current == null)
                {
                    return null;
                }
                else
                {
                    return GetSuccessor(Current, m_RootLevelNodes, m_RootLevelNodes.Last());
                }
            }
        }

        public ObservableCollection<TreeViewNode> MultiSelection
        {
            get;
            private set;
        }

        public ISet<SongMetadataTreeViewNode> Songs
        {
            get
            {
                ISet<SongMetadataTreeViewNode> result = new SortedSet<SongMetadataTreeViewNode>();

                foreach (TreeViewNode node in MultiSelection)
                {
                    InsertSongs(node, result);
                }

                return result;
            }
        }
        
        private void InsertSongs(TreeViewNode node, ISet<SongMetadataTreeViewNode> songs)
        {
            if (node is SongMetadataTreeViewNode)
            {
                songs.Add(node as SongMetadataTreeViewNode);
            }
            else
            {
                foreach (TreeViewNode child in node.Children)
                {
                    InsertSongs(child, songs);
                }
            }
        }

        private TreeViewNode GetPredecessor(TreeViewNode current, IList<TreeViewNode> search, TreeViewNode dfault)
        {
            if (search == null || search.Count == 0)
            {
                return dfault;
            }
            else
            {
                TreeViewNode best = dfault;

                foreach (TreeViewNode node in search)
                {
                    if (node.ID < current.ID)
                    {
                        best = node;
                    }
                    else
                    {
                        break;
                    }
                }

                if (best.ID < current.ID - 1 && best.IsExpanded)
                {
                    return GetPredecessor(current, best.Children, best);
                }
                else
                {
                    return best;
                }
            }            
        }

        private TreeViewNode GetSuccessor(TreeViewNode current, IList<TreeViewNode> search, TreeViewNode dfault)
        {
            if (search == null || search.Count == 0)
            {
                return dfault;
            }
            else
            {
                TreeViewNode bestBefore = null;
                TreeViewNode bestAfter = dfault;

                foreach (TreeViewNode node in search)
                {
                    if (node == current)
                    {
                        if (node.IsExpanded)
                        {
                            return GetSuccessor(current, node.Children, bestAfter);
                        }
                    }
                    else if (node.ID == current.ID + 1)
                    {
                        return node;
                    }
                    else if (node.ID > current.ID)
                    {
                        bestAfter = node;
                        break;
                    }
                    else
                    {
                        bestBefore = node;
                    }
                }

                if (bestBefore != null && bestBefore.IsExpanded)
                {
                    return GetSuccessor(current, bestBefore.Children, bestAfter);
                }
                else if (bestAfter.ID <= current.ID)
                {
                    return current;
                }
                else
                {
                    return bestAfter;
                }
            }
        }
    }
}
