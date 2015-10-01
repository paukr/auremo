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
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace Auremo
{
    public class ServerStatus : INotifyPropertyChanged
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
        private bool m_OK = false;
        private int? m_Volume = null;
        private int m_PlaylistVersion = -1;
        private int m_CurrentSongIndex = -1;
        private int m_PlayPosition = 0;
        private int m_SongLength = 0;
        private string m_State = "";
        private int m_DatabaseUpdateTime = 0;

        public ServerStatus(DataModel dataModel)
        {
            m_DataModel = dataModel;
            Reset();
            m_DataModel.ServerSession.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(OnServerSessionPropertyChanged);
        }

        public void Update()
        {
            m_DataModel.ServerSession.Status();
            m_DataModel.ServerSession.Stats();
        }

        public void OnStatusResponseReceived(IEnumerable<MPDResponseLine> response)
        {
            // Not all the values checked for below are always
            // present. Handle them specially so that defaults
            // can be provided.
            int currentSongIndex = -1;
            int playPosition = 0;
            int songLength = 0;
            string audioQuality = "";
            string errorMessage = "";

            foreach (MPDResponseLine line in response)
            {
                if (line.Key == MPDResponseLine.Keyword.State)
                {
                    State = line.Value;
                }
                else if (line.Key == MPDResponseLine.Keyword.Volume)
                {
                    int? vol = line.IntValue;
                    Volume = vol >= 0 && vol <= 100 ? vol : null;
                }
                else if (line.Key == MPDResponseLine.Keyword.Playlist)
                {
                    PlaylistVersion = line.IntValue;
                }
                else if (line.Key == MPDResponseLine.Keyword.Song)
                {
                    currentSongIndex = line.IntValue;
                }
                else if (line.Key == MPDResponseLine.Keyword.Time)
                {
                    int[] parts = line.IntListValue;

                    if (parts.Length == 2 && parts[0] >= 0 && parts[1] >= 0)
                    {
                        playPosition = parts[0];
                        songLength = parts[1];
                    }
                }
                else if (line.Key == MPDResponseLine.Keyword.Random)
                {
                    IsOnRandom = line.Value == "1";
                }
                else if (line.Key == MPDResponseLine.Keyword.Repeat)
                {
                    IsOnRepeat = line.Value == "1";
                }
                else if (line.Key == MPDResponseLine.Keyword.Single)
                {
                    IsOnSingle = line.Value == "1";
                }
                else if (line.Key == MPDResponseLine.Keyword.Consume)
                {
                    IsOnConsume = line.Value == "1";
                }
                else if (line.Key == MPDResponseLine.Keyword.Audio)
                {
                    if (line.Value == "0:?:0")
                    {
                        // A little kludge to preserve the previous audio
                        // quality text during song changes instead of
                        // flashing "0 kHz ? bps 0 channels" for a moment.
                        audioQuality = AudioQuality;
                    }
                    else
                    {
                        int[] parts = line.IntListValue;

                        if (parts.Length == 3)
                        {
                            audioQuality = parts[0] / 1000 + " kHz, " + parts[1] + " bits per sample, ";

                            if (parts[2] == 1)
                            {
                                audioQuality += "mono";
                            }
                            else if (parts[2] == 2)
                            {
                                audioQuality += "stereo";
                            }
                            else
                            {
                                audioQuality += parts[2] + " channels";
                            }
                        }
                        else
                        {
                            AudioQuality = "";
                        }
                    }
                }
                else if (line.Key == MPDResponseLine.Keyword.Xfade)
                {
                    Crossfade = line.IntValue;
                }
                else if (line.Key == MPDResponseLine.Keyword.MixRampdb)
                {
                    MixRampdb = line.DoubleValue;
                }
                else if (line.Key == MPDResponseLine.Keyword.MixRampDelay)
                {
                    MixRampDelay = line.DoubleValue;
                }
                else if (line.Key == MPDResponseLine.Keyword.Error)
                {
                    errorMessage = "Error: " + line.Value;
                }
            }

            CurrentSongIndex = currentSongIndex;
            PlayPosition = playPosition;
            SongLength = songLength;
            AudioQuality = audioQuality;
            ErrorMessage = errorMessage;
        }

        public void OnStatsResponseReceived(IEnumerable<MPDResponseLine> response)
        {
            foreach (MPDResponseLine line in response)
            {
                if (line.Key == MPDResponseLine.Keyword.DbUpdate)
                {
                    DatabaseUpdateTime = line.IntValue;
                    // We're not interested in anything else right now.
                    return;
                }
            }
        }

        public bool OK
        {
            get
            {
                return m_OK;
            }
            private set
            {
                if (m_OK != value)
                {
                    m_OK = value;
                    NotifyPropertyChanged("OK");
                }
            }
        }

        public string State
        {
            get
            {
                return m_State;
            }
            private set
            {
                if (m_State != value)
                {
                    m_State = value;
                    IsPlaying = m_State == "play";
                    IsPaused = m_State == "pause";
                    IsStopped = m_State == "stop";
                    NotifyPropertyChanged("State");
                }
            }
        }

        bool m_IsPlaying = false;
        public bool IsPlaying
        {
            get
            {
                return m_IsPlaying;
            }
            private set
            {
                if (value != m_IsPlaying)
                {
                    m_IsPlaying = value;
                    NotifyPropertyChanged("IsPlaying");
                }
            }
        }

        bool m_IsPaused = false;
        public bool IsPaused
        {
            get
            {
                return m_IsPaused;
            }
            private set
            {
                if (value != m_IsPaused)
                {
                    m_IsPaused = value;
                    NotifyPropertyChanged("IsPaused");
                }
            }
        }

        bool m_IsStopped = false;
        public bool IsStopped
        {
            get
            {
                return m_IsStopped;
            }
            private set
            {
                if (value != m_IsStopped)
                {
                    m_IsStopped = value;
                    NotifyPropertyChanged("IsStopped");
                }
            }
        }

        bool m_IsOnRepeat = false;
        public bool IsOnRepeat
        {
            get
            {
                return m_IsOnRepeat;
            }
            private set
            {
                if (value != m_IsOnRepeat)
                {
                    m_IsOnRepeat = value;
                    NotifyPropertyChanged("IsOnRepeat");
                }
            }
        }

        bool m_IsOnRandom = false;
        public bool IsOnRandom
        {
            get
            {
                return m_IsOnRandom;
            }
            private set
            {
                if (value != m_IsOnRandom)
                {
                    m_IsOnRandom = value;
                    NotifyPropertyChanged("IsOnRandom");
                }
            }
        }

        bool m_IsOnSingle = false;
        public bool IsOnSingle
        {
            get
            {
                return m_IsOnSingle;
            }
            private set
            {
                if (value != m_IsOnSingle)
                {
                    m_IsOnSingle = value;
                    NotifyPropertyChanged("IsOnSingle");
                }
            }
        }

        bool m_IsOnConsume = false;
        public bool IsOnConsume
        {
            get
            {
                return m_IsOnConsume;
            }
            private set
            {
                if (value != m_IsOnConsume)
                {
                    m_IsOnConsume = value;
                    NotifyPropertyChanged("IsOnConsume");
                }
            }
        }

        public int? Volume
        {
            get
            {
                return m_Volume;
            }
            private set
            {
                if (m_Volume != value)
                {
                    m_Volume = value;
                    NotifyPropertyChanged("Volume");
                }
            }
        }

        public int PlaylistVersion
        {
            get
            {
                return m_PlaylistVersion;
            }
            private set
            {
                if (m_PlaylistVersion != value)
                {
                    m_PlaylistVersion = value;
                    NotifyPropertyChanged("PlaylistVersion");
                }
            }
        }

        public int CurrentSongIndex
        {
            get
            {
                return m_CurrentSongIndex;
            }
            private set
            {
                if (m_CurrentSongIndex != value)
                {
                    m_CurrentSongIndex = value;
                    NotifyPropertyChanged("CurrentSongIndex");
                }
            }
        }

        public int PlayPosition
        {
            get
            {
                return m_PlayPosition;
            }
            private set
            {
                if (m_PlayPosition != value)
                {
                    m_PlayPosition = value;
                    NotifyPropertyChanged("PlayPosition");
                }
            }
        }

        public int SongLength
        {
            get
            {
                return m_SongLength;
            }
            private set
            {
                if (m_SongLength != value)
                {
                    m_SongLength = value;
                    NotifyPropertyChanged("SongLength");
                }
            }
        }

        public int DatabaseUpdateTime
        {
            get
            {
                return m_DatabaseUpdateTime;
            }
            private set
            {
                if (value != m_DatabaseUpdateTime)
                {
                    m_DatabaseUpdateTime = value;
                    NotifyPropertyChanged("DatabaseUpdateTime");
                }
            }
        }

        private string m_AudioQuality = "";
        public string AudioQuality
        {
            get
            {
                return m_AudioQuality;
            }
            private set
            {
                if (value != m_AudioQuality)
                {
                    m_AudioQuality = value;
                    NotifyPropertyChanged("AudioQuality");
                }
            }
        }

        private int m_Crossfade = 0;

        public int Crossfade
        {
            get
            {
                return m_Crossfade;
            }
            private set
            {
                if (value != m_Crossfade)
                {
                    m_Crossfade = value;
                    NotifyPropertyChanged("Crossfade");
                    NotifyPropertyChanged("CrossfadeDisplayString");
                }
            }
        }

        public string CrossfadeDisplayString
        {
            get
            {
                return "Crossfade: " + Crossfade + " seconds";
            }
        }

        private double m_MixRampdb = 0;

        public double MixRampdb
        {
            get
            {
                return m_MixRampdb;
            }
            private set
            {
                if (value != m_MixRampdb)
                {
                    m_MixRampdb = value;
                    NotifyPropertyChanged("MixRampdb");
                    NotifyPropertyChanged("MixRampdbDisplayString");
                }
            }
        }

        public string MixRampdbDisplayString => "Mix ramp threshold: " + MixRampdb.ToString(NumberFormatInfo.InvariantInfo) + " dB";

        private double m_MixRampDelay = double.NaN;

        public double MixRampDelay
        {
            get
            {
                return m_MixRampDelay;
            }
            private set
            {
                if (value != m_MixRampDelay)
                {
                    m_MixRampDelay = value;
                    NotifyPropertyChanged("MixRampDelay");
                    NotifyPropertyChanged("MixRampDelayDisplayString");
                    NotifyPropertyChanged("IsMixRampEnabled");
                }
            }
        }
        
        public string MixRampDelayDisplayString => double.IsNaN(MixRampDelay) ?
            // TODO: this is no place for References to clicking.
            "Click here to enable mix ramp" :
            "Mix ramp delay: " + MixRampDelay.ToString(NumberFormatInfo.InvariantInfo) + " seconds";

        public bool IsMixRampEnabled => !double.IsNaN(MixRampDelay);

        private string m_ErrorMessage = "";
        public string ErrorMessage
        {
            get
            {
                return m_ErrorMessage;
            }
            private set
            {
                if (value != m_ErrorMessage)
                {
                    m_ErrorMessage = value;
                    NotifyPropertyChanged("ErrorMessage");
                }
            }
        }

        private void OnServerSessionPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "State")
            {
                if (m_DataModel.ServerSession.State == ServerSession.SessionState.Connected)
                {
                    Update();
                }
                else
                {
                    Reset();
                }
            }
        }

        private void Reset()
        {
            OK = false;
            Volume = null;
            PlaylistVersion = -1;
            CurrentSongIndex = -1;
            PlayPosition = 0;
            SongLength = 0;
            IsOnRandom = false;
            IsOnRepeat = false;
            State = "stop";
            DatabaseUpdateTime = 0;
            AudioQuality = "";
            Crossfade = 0;
            MixRampdb = double.NaN;
            MixRampDelay = double.NaN;
            ErrorMessage = "";
        }
    }
}
