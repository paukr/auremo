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

using System.Globalization;

namespace Auremo
{
    public class MPDCommand
    {
        public MPDCommand(string op)
        {
            Op = op;
            Argument1 = null;
            Argument2 = null;
            FullSyntax = op;
        }

        public MPDCommand(string op, string argument1)
        {
            Op = op;
            Argument1 = argument1;
            Argument2 = null;
            FullSyntax = op + " " + Quote(argument1);
        }

        public MPDCommand(string op, int argument1)
        {
            Op = op;
            Argument1 = argument1.ToString();
            Argument2 = null;
            FullSyntax = op + " " + Quote(argument1);
        }

        public MPDCommand(string op, bool argument1)
        {
            Op = op;
            Argument1 = argument1 ? "1" : "0";
            Argument2 = null;
            FullSyntax = op + " " + Quote(Argument1);
        }

        public MPDCommand(string op, double argument)
        {
            Op = op;
            Argument1 = double.IsNaN(argument) ? "nan" : argument.ToString(NumberFormatInfo.InvariantInfo);
            Argument2 = null;
            FullSyntax = op + " " + Quote(Argument1);
        }

        public MPDCommand(string op, string argument1, string argument2)
        {
            Op = op;
            Argument1 = argument1;
            Argument2 = argument2;
            FullSyntax = op + " " + Quote(argument1) + " " + Quote(argument2);
        }

        public MPDCommand(string op, string argument1, int argument2)
        {
            Op = op;
            Argument1 = argument1;
            Argument2 = argument2.ToString();
            FullSyntax = op + " " + Quote(argument1) + " " + Quote(argument2);
        }

        public MPDCommand(string op, int argument1, int argument2)
        {
            Op = op;
            Argument1 = argument1.ToString();
            Argument2 = argument2.ToString();
            FullSyntax = op + " " + Quote(argument1) + " " + Quote(argument2);
        }

        public string Op
        {
            get;
            private set;
        }

        public string Argument1
        {
            get;
            private set;
        }

        public string Argument2
        {
            get;
            private set;
        }

        public string FullSyntax
        {
            get;
            private set;
        }

        private static string Quote(int i)
        {
            return "\"" + i.ToString() + "\"";
        }

        private static string Quote(string s)
        {
            string intermediate = s.Replace("\\", "\\\\");
            string result = intermediate.Replace("\"", "\\\"");
            return "\"" + result + "\"";
        }
    }
}
