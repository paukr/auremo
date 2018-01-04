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
using System.Collections.ObjectModel;
using System.Linq;

namespace Auremo.MusicLibrary
{
    public class HierarchyController
    {
        public HierarchyController(IList<HierarchicalLibraryItem> rootLevelNodes)
        {
            RootLevelNodes = new List<HierarchicalLibraryItem>();
            MultiSelection = new ObservableCollection<HierarchicalLibraryItem>();
            Current = null;
        }

        public void Clear()
        {
            RootLevelNodes.Clear();
            MultiSelection.Clear();
            Current = null;
        }

        /// <summary>
        /// Call after initializing nodes to give them ordered unique IDs.
        /// </summary>
        public void ResetNodeIds()
        {
            int nodeId = 0;

            foreach (HierarchicalLibraryItem root in RootLevelNodes)
            {
                nodeId = AssignNodeIdsRecursively(root, nodeId);
            }
        }

        private int AssignNodeIdsRecursively(HierarchicalLibraryItem parent, int nodeId)
        {
            parent.Id = nodeId++;

            foreach (HierarchicalLibraryItem child in parent.Children)
            {
                nodeId = AssignNodeIdsRecursively(child, nodeId);
            }

            parent.HighestChildId = nodeId - 1;
            return nodeId;
        }

        public IList<HierarchicalLibraryItem> RootLevelNodes
        {
            get;
            private set;
        }

        public HierarchicalLibraryItem FirstNode
        {
            get
            {
                if (RootLevelNodes.Count == 0)
                {
                    return null;
                }
                else
                {
                    return RootLevelNodes.First();
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

        public void SelectRange(HierarchicalLibraryItem toNode)
        {
            if (Pivot != null)
            {
                int minId = Math.Min(Pivot.Id, toNode.Id);
                int maxId = Math.Max(Pivot.Id, toNode.Id);

                foreach (HierarchicalLibraryItem root in RootLevelNodes)
                {
                    SelectVisibleWithinRange(root, minId, maxId);
                }
            }
        }

        private void SelectVisibleWithinRange(HierarchicalLibraryItem node, int minId, int maxId)
        {
            if (minId <= node.Id && node.Id <= maxId)
            {
                node.IsMultiSelected = true;
            }

            if (node.IsExpanded && node.Id < maxId && node.HighestChildId >= minId)
            {
                foreach (HierarchicalLibraryItem child in node.Children)
                {
                    SelectVisibleWithinRange(child, minId, maxId);

                    if (child.Id >= maxId)
                    {
                        return;
                    }
                }
            }
        }

        /// Start point of range selection (mouse or key with shift down).
        public HierarchicalLibraryItem Pivot
        {
            get;
            set;
        }

        public HierarchicalLibraryItem Current
        {
            get;
            set;
        }

        public HierarchicalLibraryItem CurrentOrFirstNode
        {
            get
            {
                if (Current == null)
                {
                    Current = FirstNode;
                }

                return Current;
            }
        }

        public HierarchicalLibraryItem Previous
        {
            get
            {
                if (Current == null)
                {
                    return null;
                }
                else
                {
                    return GetPredecessor(Current, RootLevelNodes, RootLevelNodes.First());
                }
            }
        }

        public HierarchicalLibraryItem Next
        {
            get
            {
                if (Current == null)
                {
                    return null;
                }
                else
                {
                    return GetSuccessor(Current, RootLevelNodes, RootLevelNodes.Last());
                }
            }
        }

        public ObservableCollection<HierarchicalLibraryItem> MultiSelection
        {
            get;
            private set;
        }

        public IEnumerable<HierarchicalLibraryItem> LeafNodes
        {
            get
            {
                IList<HierarchicalLibraryItem> result = new List<HierarchicalLibraryItem>();
                CollectLeafNodesRecursively(RootLevelNodes, result);
                return result;
            }
        }

        private void CollectLeafNodesRecursively(IEnumerable<HierarchicalLibraryItem> children, IList<HierarchicalLibraryItem> result)
        {
            foreach (HierarchicalLibraryItem child in children)
            {
                if (child.Children.Count == 0)
                {
                    result.Add(child);
                }
                else
                {
                    CollectLeafNodesRecursively(child.Children, result);
                }
            }
        }

        public IEnumerable<LibraryItem> SelectedLeaves
        {
            get
            {
                IList<LibraryItem> result = new List<LibraryItem>();

                foreach (HierarchicalLibraryItem node in MultiSelection)
                {
                    InsertLeavesRecursively(node, result);
                }

                return result;
            }
        }
        
        private void InsertLeavesRecursively(HierarchicalLibraryItem node, IList<LibraryItem> leaves)
        {
            if (node.Children.Count == 0)
            {
                leaves.Add(node.Item);
            }
            else
            {
                foreach (HierarchicalLibraryItem child in node.Children)
                {
                    InsertLeavesRecursively(child, leaves);
                }
            }
        }

        private HierarchicalLibraryItem GetPredecessor(HierarchicalLibraryItem current, IList<HierarchicalLibraryItem> search, HierarchicalLibraryItem dfault)
        {
            if (search == null || search.Count == 0)
            {
                return dfault;
            }
            else
            {
                HierarchicalLibraryItem best = dfault;

                foreach (HierarchicalLibraryItem node in search)
                {
                    if (node.Id < current.Id)
                    {
                        best = node;
                    }
                    else
                    {
                        break;
                    }
                }

                if (best.Id < current.Id - 1 && best.IsExpanded)
                {
                    return GetPredecessor(current, best.Children, best);
                }
                else
                {
                    return best;
                }
            }            
        }

        private HierarchicalLibraryItem GetSuccessor(HierarchicalLibraryItem current, IList<HierarchicalLibraryItem> search, HierarchicalLibraryItem dfault)
        {
            if (search == null || search.Count == 0)
            {
                return dfault;
            }
            else
            {
                HierarchicalLibraryItem bestBefore = null;
                HierarchicalLibraryItem bestAfter = dfault;

                foreach (HierarchicalLibraryItem node in search)
                {
                    if (node == current)
                    {
                        if (node.IsExpanded)
                        {
                            return GetSuccessor(current, node.Children, bestAfter);
                        }
                    }
                    else if (node.Id == current.Id + 1)
                    {
                        return node;
                    }
                    else if (node.Id > current.Id)
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
                else if (bestAfter.Id <= current.Id)
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
