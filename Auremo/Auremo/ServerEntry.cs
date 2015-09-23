using System;
using System.ComponentModel;
using System.Configuration;

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

        public ServerEntry()
        {
            ItemIndex = 0;
        }

        public ServerEntry(string hostname, int port, string encryptedPassword, int index = 0, bool selected = false)
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
        public ServerEntry(ServerEntry model, int index = 0, bool selected = false)
        {
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
