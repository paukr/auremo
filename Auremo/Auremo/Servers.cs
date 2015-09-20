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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
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
}
