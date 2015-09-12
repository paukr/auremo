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
using System.Xml;
using System.Xml.Serialization;

namespace Auremo
{
    public class Servers : INotifyPropertyChanged
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

        public Servers()
        {
            Items = new ObservableCollection<Server>();
            Items.Add(new Server("localhost", 6600, null));
            Items[0].IsSelected = true;
        }

        public void SetItems(IEnumerable<Server> items, int selectedIndex)
        {
            Items.Clear();

            foreach (Server item in items)
            {
                Server server = new Server(item.Hostname, item.Port, item.EncryptedPassword);
                server.IsSelected = Items.Count == selectedIndex;
                Items.Add(server);
            }

            // Have something by default
            if (Items.Count == 0)
            {
                Items.Add(new Server("localhost", 6600, null));
                Items[0].IsSelected = true;
            }
        }

        public void SetSelectedItem(int index)
        {
            Items[m_SelectedServerIndex].IsSelected = false;
            m_SelectedServerIndex = Utils.Clamp(0, index, Items.Count - 1);
            Items[m_SelectedServerIndex].IsSelected = true;
        }

        public ObservableCollection<Server> Items
        {
            get;
            private set;
        }

        public Server SelectedServer
        {
            get
            {
                return Items[m_SelectedServerIndex];
            }
        }

        public string ExportToXml()
        {
            Server[] items = new Server[Items.Count];
            Items.CopyTo(items, 0);

            XmlSerializer serializer = new XmlSerializer(typeof(Server[]));
            TextWriter writer = new StringWriter();
            serializer.Serialize(writer, items);
            return writer.ToString();
        }

        public void ImportFromXml(string xml)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Server[]));
            TextReader reader = new StringReader(xml);
            Server[] items = serializer.Deserialize(reader) as Server[];
            bool selectedItemFound = false;

            if (items.Length == 0)
            {
                Items.Clear();
                Server server = new Server("localhost", 6600, "");
                server.IsSelected = true;
                Items.Add(server);
            }
            else
            {

                foreach (Server server in items)
                {
                    if (selectedItemFound)
                    {
                        server.IsSelected = false;
                    }
                    else
                    {
                        selectedItemFound = server.IsSelected;
                    }
                }

                //if () ;
            }
        }
    }

    [Serializable()]
    public class Server : INotifyPropertyChanged
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

        public Server()
        {
            Hostname = "localhost";
            Port = 6600;
            EncryptedPassword = "";
            IsSelected = false;
        }

        public Server(string hostname, int port, string encryptedPassword)
        {
            Hostname = hostname;
            Port = port;
            EncryptedPassword = encryptedPassword;
            IsSelected = false;
        }

        [XmlElement("Hostname")]
        public string Hostname { get; set; }

        [XmlElement("Port")]
        public int Port { get; set; }

        [XmlElement("Password")]
        public string EncryptedPassword { get; set; }

        [XmlIgnore]
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

        public string DisplayString
        {
            get
            {
                return Hostname + ":" + Port;
            }
        }
    }
}
