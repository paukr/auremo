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
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using Auremo.Properties;

namespace Auremo
{
    public class StreamsCollection : INotifyPropertyChanged
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

        private IDictionary<string, StreamMetadata> m_StreamsByLabel = new SortedDictionary<string, StreamMetadata>(StringComparer.CurrentCulture);
        const string m_Filename = "saved_streams.pls";

        public StreamsCollection()
        {
            Streams = new ObservableCollection<MusicCollectionItem>();
            Load();
        }

        public IList<MusicCollectionItem> Streams
        {
            get;
            private set;
        }

        public void Load()
        {
            m_StreamsByLabel.Clear();
            IsolatedStorageFile store = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly | IsolatedStorageScope.Domain, null, null);

            if (store.FileExists(m_Filename))
            {
                IsolatedStorageFileStream file = store.OpenFile(m_Filename, System.IO.FileMode.Open);
                byte[] data = new byte[file.Length];
                int bytesRead = file.Read(data, 0, data.Length);

                if (bytesRead == data.Length)
                {
                    PLSParser parser = new PLSParser();
                    string playlist = System.Text.Encoding.UTF8.GetString(data);
                    IEnumerable<StreamMetadata> streams = parser.ParseString(playlist);

                    if (streams != null)
                    {
                        foreach (StreamMetadata stream in streams)
                        {
                            AddWithoutNotification(stream);
                        }

                        UpdateStreamsView();
                    }
                }
            }

            store.Close();
        }

        public void Save()
        {
            IsolatedStorageFile store = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly | IsolatedStorageScope.Domain, null, null);

            if (m_StreamsByLabel.Count > 0)
            {
                IsolatedStorageFileStream file = store.OpenFile(m_Filename, System.IO.FileMode.Create);
                string playlist = PlaylistWriter.Write(m_StreamsByLabel.Values);
                byte[] data = System.Text.Encoding.UTF8.GetBytes(playlist);
                file.Write(data, 0, data.Length);
                file.Close();
            }
            else
            {
                try
                {
                    store.DeleteFile(m_Filename);
                }
                catch (Exception)
                {
                }
            }

            store.Close();
        }

        public bool Add(StreamMetadata stream)
        {
            if (AddWithoutNotification(stream))
            {
                Save();
                UpdateStreamsView();
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Add(IEnumerable<StreamMetadata> streams)
        {
            bool allSucceeded = true;

            foreach (StreamMetadata stream in streams)
            {
                bool succeeded = AddWithoutNotification(stream);
                allSucceeded &= succeeded;
            }

            Save();
            UpdateStreamsView();
            return allSucceeded;
        }
        
        public bool Delete(MusicCollectionItem item)
        {
            if (item != null && item.Content is StreamMetadata)
            {
                StreamMetadata stream = item.Content as StreamMetadata;

                if (DeleteWithoutNotification(stream))
                {
                    Save();
                    UpdateStreamsView();
                    return true;
                }
            }
            
            return false;
        }
        
        public bool Delete(IEnumerable<MusicCollectionItem> items)
        {
            bool allSucceeded = true;

            foreach (MusicCollectionItem item in items)
            {
                if (item != null && item.Content is StreamMetadata)
                {
                    StreamMetadata stream = item.Content as StreamMetadata;
                    bool succeeded = DeleteWithoutNotification(stream);
                    allSucceeded &= succeeded;
                }
            }

            Save();
            UpdateStreamsView();
            return allSucceeded;
        }
        
        public bool Rename(StreamMetadata stream, string newLabel)
        {
            if (m_StreamsByLabel.ContainsKey(newLabel))
            {
                return false;
            }
            else
            {
                m_StreamsByLabel.Remove(stream.Label);
                stream.Label = newLabel;
                m_StreamsByLabel.Add(stream.Label, stream);
                Save();
                UpdateStreamsView();
                return true;
            }
        }

        public StreamMetadata StreamByPath(string path)
        {
            foreach (StreamMetadata stream in m_StreamsByLabel.Values)
            {
                if (stream.Path == path)
                {
                    return stream;
                }
            }

            return null;
        }

        private bool AddWithoutNotification(StreamMetadata stream)
        {
            if (m_StreamsByLabel.ContainsKey(stream.Label))
            {
                return false;
            }

            m_StreamsByLabel.Add(stream.Label, stream);
            return true;
        }

        private bool DeleteWithoutNotification(StreamMetadata stream)
        {
            if (m_StreamsByLabel.ContainsKey(stream.Label))
            {
                m_StreamsByLabel.Remove(stream.Label);
                return true;    
            }

            return false;
        }

        private void UpdateStreamsView()
        {
            Streams.Clear();

            foreach (StreamMetadata stream in m_StreamsByLabel.Values)
            {
                Streams.Add(new MusicCollectionItem(stream, Streams.Count));
            }
        }
    }
}
