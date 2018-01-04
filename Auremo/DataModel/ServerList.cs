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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

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

        public ServerList()
        {
            // Have something by default.
            Items = new ObservableCollection<ServerEntry> { new ServerEntry(this, "localhost", 6600, "", 0, true) };
        }

        public void SetItems(IEnumerable<ServerEntry> items, int selectedIndex)
        {
            Items.Clear();

            if (items.Count() == 0)
            {
                // Have something by default.
                Items.Add(new ServerEntry(this, "localhost", 6600, "", 0, true));
            }
            else
            {
                foreach (ServerEntry item in items)
                {
                    Items.Add(new ServerEntry(this, item, Items.Count, Items.Count == selectedIndex));
                }
            }

            NotifyPropertyChanged("SelectedServerIndex");
            NotifyPropertyChanged("SelectedServer");
        }

        public int SelectedServerIndex
        {
            get
            {
                foreach (ServerEntry server in Items)
                {
                    if (server.IsSelected)
                    {
                        return server.ItemIndex;
                    }
                }

                return -1;
            }
        }

        private void SetSelectedServerIndex(int index)
        {
            foreach (ServerEntry server in Items)
            {
                server.IsSelected = server.ItemIndex == index;
            }

            NotifyPropertyChanged("SelectedServerIndex");
            NotifyPropertyChanged("SelectedServer");
        }

        public void OnSelectedItemChanged(ServerEntry caller)
        {
            NotifyPropertyChanged("SelectedServerIndex");
            NotifyPropertyChanged("SelectedServer");
        }

        public ObservableCollection<ServerEntry> Items
        {
            get;
            private set;
        }

        public ServerEntry SelectedServer => SelectedServerIndex < 0 ? null : Items[SelectedServerIndex];

        public void Add(string hostname, int port, string encryptedPassword)
        {
            Items.Add(new ServerEntry(this, hostname, port, encryptedPassword, Items.Count, false));
            SetSelectedServerIndex(Items.Count - 1);
        }

        public void RemoveSelected()
        {
            int index = SelectedServerIndex;

            if (Items.Count > 1 && index >= 0)
            {
                SetSelectedServerIndex(-1);
                Items.RemoveAt(index);

                for (int i = 0; i < Items.Count; ++i)
                {
                    Items[i].ItemIndex = i;
                }

                SetSelectedServerIndex(Utils.Clamp(0, index - 1, Items.Count - 1));
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
                        ServerEntry server = new ServerEntry(this, parts[i], Utils.StringToInt(parts[i + 1], -1), parts[i + 2]);
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
                servers = new ServerEntry[] { new ServerEntry(this, "localhost", 6600, "", 0, false) };
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
    }
}
