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

using System.ComponentModel;
using Auremo.Properties;

namespace Auremo
{
    public class ServerSession : INotifyPropertyChanged
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

        private DataModel m_DataModel = null;
        private ServerSessionThread m_SessionThread = null;
        private SessionState m_State = SessionState.Disconnected;
        private string m_Host = "";
        private int m_Port = -1;
        private bool m_OnlineMode = true;
        private string m_StatusMessage = "Initializing.";
        private string m_ActivityDescription = "";
        private string m_ErrorMessage = "";

        private delegate void ThreadMessage(string message);
        private delegate void ThreadState(SessionState state);
        
        public enum SessionState
        {
            Disconnected,            // Not connected and idle
            Connecting,              // (Re-)establishing connection
            Connected,               // Connection OK
            Disconnecting            // Disconnecting and not reconnecting when done
        }

        public ServerSession(DataModel dataModel)
        {
            m_DataModel = dataModel;
            m_DataModel.MainWindow.GlobalUpdateEvent += OnGlobalUpdate;
            m_DataModel.ServerList.PropertyChanged += OnServersPropertyChanged;
        }
        
        private bool Connect()
        {
            if (m_SessionThread != null)
            {
                return false;
            }

            m_Host = m_DataModel.ServerList.SelectedServer.Hostname;
            m_Port = m_DataModel.ServerList.SelectedServer.Port;
            m_SessionThread = new ServerSessionThread(this, m_DataModel, 1000 * Settings.Default.NetworkTimeout, Settings.Default.ReconnectInterval);
            m_SessionThread.Start();
            return true;
        }

        public void Disconnect()
        {
            if (m_SessionThread != null)
            {
                m_SessionThread.Terminating = true;
            }
        }
        
        public bool OnlineMode
        {
            get
            {
                return m_OnlineMode;
            }
            set
            {
                m_OnlineMode = value;

                if (OnlineMode)
                {
                    Connect();
                }
                else
                {
                    Disconnect();
                }
            }
        }
         
        public SessionState State
        {
            get
            {
                return m_State;
            }
            set
            {
                if (value != m_State)
                {
                    ErrorMessage = "";
                    m_State = value;
                    NotifyPropertyChanged("State");
                    NotifyPropertyChanged("IsConnected");
                    UpdateStatusMessage();
                }
            }
        }

        public bool IsConnected
        {
            get
            {
                return State == SessionState.Connected;
            }
        }

        public void Send(MPDCommand command)
        {
            if (m_SessionThread != null || m_State == SessionState.Connected)
            {
                m_SessionThread.Send(command);
            }
        }

        public void Send(MPDCommandList commands)
        {
            if (m_SessionThread != null || m_State == SessionState.Connected)
            {
                m_SessionThread.Send(commands);
            }
        }

        private void OnGlobalUpdate()
        {
            DoCleanup();

            if (OnlineMode && State == SessionState.Disconnected)
            {
                Connect();
            }
        }

        private void DoCleanup()
        {
            if (m_State == SessionState.Disconnecting && m_SessionThread.Join())
            {
                m_SessionThread = null;
                m_State = SessionState.Disconnected;
            }
        }

        private void OnServersPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedServer")
            {
                Disconnect();
            }
        }

        public void OnConnectionStateChanged(SessionState state)
        {
            m_DataModel.MainWindow.Dispatcher.Invoke(new ThreadState((SessionState s) => { State = s; }), state);
        }

        public void OnActivityChanged(string activity)
        {
            m_DataModel.MainWindow.Dispatcher.Invoke(new ThreadMessage((string a) => { ActivityDescription = a; }), activity);
        }

        public void OnErrorMessageChanged(string error)
        {
            m_DataModel.MainWindow.Dispatcher.Invoke(new ThreadMessage((string e) => { ErrorMessage = e; }), error);
        }

        #region Status message construction

        public string StatusMessage
        {
            get
            {
                return m_StatusMessage;
            }
            set
            {
                if (value != m_StatusMessage)
                {
                    m_StatusMessage = value;
                    NotifyPropertyChanged("StatusMessage");
                }
            }
        }

        private string ConnectionState
        {
            get
            {
                if (m_Host == "")
                {
                    return ""; // Not initialized yet, most likely.
                }
                else if (m_State == SessionState.Connecting)
                {
                    return "Connecting to " + m_Host + ":" + m_Port + ".";
                }
                else if (m_State == SessionState.Connected)
                {
                    return "Connected to " + m_Host + ":" + m_Port + ".";
                }
                else if (m_State == SessionState.Disconnecting)
                {
                    return "Disconnecting from " + m_Host + ":" + m_Port + ".";
                }
                else
                {
                    return "Disconnected from " + m_Host + ":" + m_Port + ".";
                }
            }
        }

        private string ActivityDescription
        {
            get
            {
                return m_ActivityDescription;
            }
            set
            {
                if (value != m_ActivityDescription)
                {
                    m_ActivityDescription = value;
                    UpdateStatusMessage();
                }
            }
        }

        private string ErrorMessage
        {
            get
            {
                return m_ErrorMessage;
            }
            set
            {
                if (value != m_ErrorMessage)
                {
                    m_ErrorMessage = value;
                    UpdateStatusMessage();
                }
            }
        }

        private void UpdateStatusMessage()
        {
            if (ErrorMessage.Length > 0)
            {
                StatusMessage = ErrorMessage;
            }
            else if (ActivityDescription.Length > 0)
            {
                StatusMessage = ActivityDescription;
            }
            else
            {
                StatusMessage = ConnectionState;
            }
        }

        #endregion
    }
}
