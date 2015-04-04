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
    /// Wraps a directory (aka folder) name so that it can be consumed by a
    /// TreeView[Item].
    /// </summary>
    public class DirectoryTreeViewNode : TreeViewNode
    {
        private string m_DirectoryName = "";

        public DirectoryTreeViewNode(string name, TreeViewNode parent, TreeViewController controller) : base(parent, controller)
        {
            m_DirectoryName = name;
        }

        public string DirectoryName
        {
            get
            {
                return m_DirectoryName;
            }
        }

        public string FullPath
        {
            get
            {
                if (Parent == null)
                {
                    return "";
                }
                else
                {
                    string root = Parent.ToString();

                    if (root == "")
                    {
                        return m_DirectoryName;
                    }
                    else
                    {
                        return root + "/" + m_DirectoryName;
                    }
                }
            }
        }

        public override string DisplayString
        {
            get
            {
                return m_DirectoryName;
            }
        }

        public override string ToString()
        {
            return FullPath;
        }
    }
}
