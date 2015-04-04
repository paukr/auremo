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
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Auremo
{
    public class Output : INotifyPropertyChanged
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

        public Output(int index, string name, bool isEnabled)
        {
            Index = index;
            Name = name;
            IsEnabled = isEnabled;
        }
        
        public int Index
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            private set;
        }

        private bool m_IsEnabled = false;

        public bool IsEnabled
        {
            get
            {
                return m_IsEnabled;
            }
            set
            {
                if (value != m_IsEnabled)
                {
                    m_IsEnabled = value;
                    NotifyPropertyChanged("IsEnabled");
                }
            }
        }

        public override string ToString()
        {
            return Index + ": " + Name + " = " + (IsEnabled ? "on" : "off");
        }
    }
}
