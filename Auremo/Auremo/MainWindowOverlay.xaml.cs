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
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Auremo
{
    /// <summary>
    /// Interaction logic for MainWindowOverlay.xaml
    /// </summary>
    public partial class MainWindowOverlay : UserControl, INotifyPropertyChanged
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

        public delegate void OverlayClosingCallback(bool ok, string input);

        private bool m_Active = false;
        private string m_Caption = "";
        private string m_Input = "";

        public MainWindowOverlay()
        {
            InitializeComponent();
            DataContext = this;
            Callback = null;
            Data = null;
        }

        public void Activate(string caption, string defaultInput, OverlayClosingCallback callback, object data = null)
        {
            Caption = caption;
            Input = defaultInput;
            Callback = callback;
            Data = data;
            Active = true;
            
        }

        public void Deactivate()
        {
            Active = false;
            Caption = "";
            Input = "";
            Callback = null;
            Data = null;
        }

        public bool Active
        {
            get
            {
                return m_Active;
            }
            set
            {
                if (value != m_Active)
                {
                    m_Active = value;
                    Visibility = m_Active ? Visibility.Visible : Visibility.Collapsed;
                    NotifyPropertyChanged("Active");
                }
            }
        }

        public string Caption
        {
            get
            {
                return m_Caption;
            }
            set
            {
                if (value != m_Caption)
                {
                    m_Caption = value;
                    NotifyPropertyChanged("Caption");
                }
            }
        }

        public string Input
        {
            get
            {
                return m_Input;
            }
            set
            {
                if (value != m_Input)
                {
                    m_Input = value;
                    NotifyPropertyChanged("Input");
                }
            }
        }

        public OverlayClosingCallback Callback
        {
            get;
            set;
        }

        /// <summary>
        /// Extra data that the caller can store for this query.
        /// </summary>
        public object Data
        {
            get;
            set;
        }

        private void OnOK(object sender, RoutedEventArgs e)
        {
            if (Callback == null)
            {
                Active = false;
            }
            else
            {
                Callback(true, Input);
            }
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            if (Callback == null)
            {
                Active = false;
            }
            else
            {
                Callback(false, Input);
            }
        }

        private void OnBackgroundClicked(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible)
            {
                Keyboard.Focus(m_InputBox);
                m_InputBox.CaretIndex = m_InputBox.Text.Length;
            }
        }
    }
}
