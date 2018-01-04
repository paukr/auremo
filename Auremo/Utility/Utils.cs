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

namespace Auremo
{
    public class Utils
    {
        public static int? StringToInt(string s)
        {
            int result = 0;

            if (int.TryParse(s, out result))
            {
                return result;
            }
            else
            {
                return null;
            }
        }

        public static int StringToInt(string s, int dfault)
        {
            int result = 0;

            if (int.TryParse(s, out result))
            {
                return result;
            }
            else
            {
                return dfault;
            }
        }

        public static double? StringToDouble(string s)
        {
            double result = 0;

            if (double.TryParse(s, out result))
            {
                return result;
            }
            else
            {
                return null;
            }
        }

        public static double StringToDouble(string s, double dfault)
        {
            double result = dfault;

            if (double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.NumberFormatInfo.InvariantInfo, out result))
            {
                return result;
            }
            else
            {
                return dfault;
            }
        }

        public static string IntToTimecode(int seconds)
        {
            if (seconds < 0)
                return "00";

            int secs = seconds % 60;
            int mins = seconds / 60 % 60;
            int hours = seconds / 3600;

            string result = "";

            if (hours > 0)
                result = hours + ":";

            if (mins < 10)
                result += "0" + mins + ":";
            else
                result += mins + ":";

            if (secs < 10)
                result += "0" + secs;
            else
                result += secs;

            return result;
        }

        public static int Clamp(int min, int value, int max)
        {
            return min < value ? (max > value ? value : max) : min;
        }

        public static string ExtractYearFromDateString(string date)
        {
            if (date == null)
            {
                return null;
            }
            else
            {
                return date.Substring(0, 4);
            }
        }
    }
}
