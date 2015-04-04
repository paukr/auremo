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

namespace Auremo
{
    /// <summary>
    /// Wraps a SongMetadata object so that it can be consumed by a TreeView[Item].
    /// </summary>
    public class SongMetadataTreeViewNode : TreeViewNode
    {
        private string m_Filename = "";
        private bool m_ParentIsDirectory = false;

        public SongMetadataTreeViewNode(string filename, SongMetadata song, TreeViewNode parent, TreeViewController controller) : base(parent, controller)
        {
            m_Filename = filename;
            m_ParentIsDirectory = Parent == null || Parent is DirectoryTreeViewNode;
            Song = song;
        }

        public SongMetadata Song
        {
            get;
            private set;
        }

        public override string DisplayString
        {
            get
            {
                return m_ParentIsDirectory ? m_Filename : Song.Title;
            }
        }

        public override void AddChild(TreeViewNode child)
        {
            throw new Exception("Attempt to add a child to a SongMetadataTreeViewNode.");
        }

        public override string ToString()
        {
            return Parent.ToString() + "/" + DisplayString;
        }
    }
}