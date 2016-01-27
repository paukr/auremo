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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Auremo
{
    // A standard button with an extra property "IsDown" for buttons that have a
    // server-dependent glow state.
    public class StickyButton : Button
    {
        public bool IsDown
        {
            get
            {
                return (bool)GetValue(IsDownProperty);
            }
            set
            {
                SetValue(IsDownProperty, value);
            }
        }

        public static readonly DependencyProperty IsDownProperty = DependencyProperty.Register("IsDown", typeof(bool), typeof(StickyButton), new UIPropertyMetadata(false));
    }
}
