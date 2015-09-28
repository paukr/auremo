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
using System.IO;
using System.Linq;
using System.Text;

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

        public static string EncodeFilename(string source)
        {
            string encodeChars = new string(Path.GetInvalidFileNameChars()) + "_";
            StringBuilder result = new StringBuilder();

            foreach (char c in source)
            {
                if (encodeChars.Contains(c))
                {
                    result.Append("_");
                    result.Append(((UInt32)c).ToString("X8"));
                }
                else
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }

        public static string DecodeFilename(string source)
        {
            StringBuilder result = new StringBuilder();
            int i = 0, max = source.Length;

            while (i < max)
            {
                if (source[i] == '_')
                {
                    if (i > max - 9)
                    {
                        throw new Exception("Improper encoding in cover art file name.");
                    }
                    else
                    {
                        string number = source.Substring(i + 1, 8);
                        i += 8;
                        UInt32 code = 0;

                        if (!UInt32.TryParse(number, out code))
                        {
                            char c = (char)code;
                            result.Append(c);
                        }
                    }
                }
                else
                {
                    result.Append(source[i]);
                }

                ++i;
            }

            return result.ToString();
        }
    }
}
