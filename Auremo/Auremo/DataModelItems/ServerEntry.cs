/*
 * Copyright 2015 Mikko Teräs and Niilo Säämänen.
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
using System.ComponentModel;

namespace Auremo
{
    [Serializable()]
    public class ServerEntry : INotifyPropertyChanged
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
        ServerList m_Parent = null;

        public ServerEntry()
        {
            ItemIndex = 0;
        }

        public ServerEntry(ServerList parent, string hostname, int port, string encryptedPassword, int index = 0, bool selected = false)
        {
            m_Parent = parent;
            Hostname = hostname;
            Port = port;
            EncryptedPassword = encryptedPassword;
            ItemIndex = index;
            IsSelected = selected;
        }

        /// <summary>
        /// Make of a copy of the model.
        /// </summary>
        public ServerEntry(ServerList parent, ServerEntry model, int index = 0, bool selected = false)
        {
            m_Parent = parent;
            Hostname = model.Hostname;
            Port = model.Port;
            EncryptedPassword = model.EncryptedPassword;
            ItemIndex = index;
            IsSelected = selected;
        }

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

        public int ItemIndex
        {
            get;
            set;
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

                    if (m_IsSelected && m_Parent != null)
                    {
                        m_Parent.OnSelectedItemChanged(this);
                    }
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
