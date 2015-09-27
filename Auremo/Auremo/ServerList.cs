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
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Auremo
{
    public class ServerList : INotifyPropertyChanged
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

        int m_SelectedServerIndex = 0;

        public ServerList()
        {
            // Have something by default.
            Items = new ObservableCollection<ServerEntry> { new ServerEntry("localhost", 6600, "", 0, true) };
        }

        public void SetItems(IEnumerable<ServerEntry> items, int selectedIndex)
        {
            Items.Clear();

            if (items.Count() == 0)
            {
                // Have something by default.
                Items.Add(new ServerEntry("localhost", 6600, "", 0, true));
                SelectedServerIndex = 0;
            }
            else
            {
                foreach (ServerEntry item in items)
                {
                    Items.Add(new ServerEntry(item, Items.Count, false));
                }

                SelectedServerIndex = NormalizeIndex(selectedIndex);
            }

            NotifyPropertyChanged("SelectedServerIndex");
            NotifyPropertyChanged("SelectedServer");
        }

        public int SelectedServerIndex
        {
            get
            {
                return m_SelectedServerIndex;
            }
            set
            {
                int index = NormalizeIndex(value);

                if (index != m_SelectedServerIndex)
                {
                    if (m_SelectedServerIndex == NormalizeIndex(m_SelectedServerIndex))
                    {
                        Items[m_SelectedServerIndex].IsSelected = false;
                    }

                    m_SelectedServerIndex = index;
                    Items[m_SelectedServerIndex].IsSelected = true;
                    NotifyPropertyChanged("SelectedServerIndex");
                    NotifyPropertyChanged("SelectedServer");
                }
            }
        }

        public ObservableCollection<ServerEntry> Items
        {
            get;
            private set;
        }

        public ServerEntry SelectedServer
        {
            get
            {
                return Items[m_SelectedServerIndex];
            }
        }

        public void Set(int index, ServerEntry server)
        {
            // Support adding as new entry
            if (index == Items.Count)
            {
                Items.Add(new ServerEntry(server, index, false));
                SelectedServerIndex = index;
            }
            else if (index < Items.Count)
            {
                Items[index] = new ServerEntry(server, index, false);
                SelectedServerIndex = index;
            }
        }

        public void Deserialize(string source)
        {
            IList<ServerEntry> servers = null;
            int selectedIndex = 0;

            if (source != null && source != "")
            {
                string[] parts = source.Split(';');

                if (parts.Length % 3 == 1)
                {
                    servers = new List<ServerEntry>();
                    selectedIndex = Utils.StringToInt(parts[0], -1);
                    bool success = selectedIndex != -1;

                    for (int i = 1; i < parts.Length && success; i += 3)
                    {
                        ServerEntry server = new ServerEntry(parts[i], Utils.StringToInt(parts[i + 1], -1), parts[i + 2]);
                        success = server.Port != -1;
                        servers.Add(server);
                    }

                    if (!success)
                    {
                        servers = null;
                    }
                }
            }

            if (servers == null)
            {
                servers = new ServerEntry[] { new ServerEntry("localhost", 6600, "", 0, false) };
            }

            SetItems(servers, selectedIndex);
        }

        public string Serialize()
        {
            StringBuilder result = new StringBuilder();
            result.Append(SelectedServerIndex);

            foreach (ServerEntry server in Items)
            {
                result.Append(';');
                result.Append(server.Hostname);
                result.Append(';');
                result.Append(server.Port);
                result.Append(';');
                result.Append(server.EncryptedPassword);
            }

            return result.ToString();
        }

        private int NormalizeIndex(int i)
        {
            return i >= 0 && i < Items.Count ? i : 0;
        }
    }
}
