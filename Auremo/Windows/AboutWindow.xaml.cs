﻿/*
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Auremo
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        private MainWindow m_Parent = null;

        public AboutWindow()
        {
            InitializeComponent();
        }

        public AboutWindow(MainWindow parent)
        {
            InitializeComponent();

            m_Parent = parent;
            Version ver = Assembly.GetExecutingAssembly().GetName().Version;
            m_VersionNumber.Text = "Version " + ver.Major + "." + ver.Minor + "." + ver.Build;
        }

        private void OnCloseClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnClose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            m_Parent.OnChildWindowClosing(this);
        }
    }
}
