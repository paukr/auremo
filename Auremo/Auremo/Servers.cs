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
            Items = new ObservableCollection<Server> { new Server("localhost", 6600, "", 0, true) };
        }

        public void SetItems(IEnumerable<Server> items, int selectedIndex)
        {
            Items.Clear();

            if (items.Count() == 0)
            {
                // Have something by default
                Items.Add(new Server("localhost", 6600, "", 0, true));
                SelectedServerIndex = 0;
            }
            else
            {
                foreach (Server item in items)
                {
                    Items.Add(new Server(item.Hostname, item.Port, item.EncryptedPassword, Items.Count, Items.Count == selectedIndex));
                }

                SelectedServerIndex = selectedIndex;
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
                int normalized = Utils.Clamp(0, value, Items.Count - 1);

                if (m_SelectedServerIndex != normalized)
                {
                    Items[m_SelectedServerIndex].IsSelected = false;
                    m_SelectedServerIndex = normalized;
                    Items[m_SelectedServerIndex].IsSelected = true;
                    NotifyPropertyChanged("SelectedServerIndex");
                    NotifyPropertyChanged("SelectedServer");
                }
            }
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

        public static IEnumerable<Server> ReadFromXml(string xml)
        {
            Server[] results = null;

            if (xml != null && xml != "")
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(Server[]));
                    TextReader reader = new StringReader(xml);
                    results = serializer.Deserialize(reader) as Server[];
                }
                catch
                {
                }
            }

            if (results == null || results.Length == 0)
            {
                results = new Server[] { new Server("localhost", 6600, "", 0, false) };
            }
            else
            {
                for (int i = 0; i < results.Length; ++i)
                {
                    results[i].ItemIndex = i;
                    results[i].IsSelected = false;
                }
            }

            return results;
        }

        public static string WriteToXml(IEnumerable<Server> items)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Server[]));
            TextWriter writer = new StringWriter();
            serializer.Serialize(writer, items.ToArray());
            return writer.ToString();
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

        private string m_Hostname = "localhost";
        private int m_Port = 6600;
        private string m_EncryptedPassword = "";
        private bool m_IsSelected = false;

        public Server()
        {
            ItemIndex = -1;
        }

        public Server(string hostname, int port, string encryptedPassword, int index = -1, bool selected = false)
        {
            Hostname = hostname;
            Port = port;
            EncryptedPassword = encryptedPassword;
            ItemIndex = index;
            IsSelected = selected;
        }

        /// <summary>
        /// Make of a copy of the model.
        /// </summary>
        public Server(Server model, int index = -1, bool selected = false)
        {
            Hostname = model.Hostname;
            Port = model.Port;
            EncryptedPassword = model.EncryptedPassword;
            ItemIndex = index;
            IsSelected = selected;
        }

        [XmlElement("Hostname")]
        public string Hostname
        {
            get
            {
                return m_Hostname;
            }
            set
            {
                if (value != m_Hostname)
                {
                    m_Hostname = value;
                    NotifyPropertyChanged("Hostname");
                }
            }
        }

        [XmlElement("Port")]
        public int Port
        {
            get
            {
                return m_Port;
            }
            set
            {
                if (value != m_Port)
                {
                    m_Port = value;
                    NotifyPropertyChanged("Port");
                }
            }
        }

        [XmlElement("Password")]
        public string EncryptedPassword
        {
            get
            {
                return m_EncryptedPassword;
            }
            set
            {
                if (value != m_EncryptedPassword)
                {
                    m_EncryptedPassword = value;
                    NotifyPropertyChanged("EncryptedPassword");
                }
            }
        }

        [XmlIgnore]
        public int ItemIndex
        {
            get;
            set;
        }

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
