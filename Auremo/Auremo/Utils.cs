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

        public static Tuple<string, string> SplitPath(string path)
        {
            int limit = path.LastIndexOf('/');

            if (limit == -1)
            {
                return new Tuple<string, string>("", path);
            }
            else
            {
                return new Tuple<string, string>(path.Substring(0, limit), path.Substring(limit + 1));
            }
        }

        public static int Clamp(int min, int value, int max)
        {
            return min < value ? (max > value ? value : max) : min;
        }

        public static IList<T> ToTypedList<T>(System.Collections.IEnumerable source)
        {
            IList<T> result = new List<T>();

            foreach (object o in source)
            {
                try
                {
                    result.Add((T)o);
                }
                catch (Exception)
                {
                    throw new Exception("ToTypedList: attempted to cast " + o.GetType().ToString() + " to " + typeof(T).ToString() + ".");
                }
            }

            return result;
        }

        public static IList<T> ToContentList<T>(System.Collections.IEnumerable source)
        {
            IList<MusicCollectionItem> collectionItems = ToTypedList<MusicCollectionItem>(source);
            IList<T> result = new List<T>();

            foreach (MusicCollectionItem item in collectionItems)
            {
                try
                {
                    result.Add((T)item.Content);
                }
                catch (Exception)
                {
                    throw new Exception("ToTypedList: attempted to cast " + item.Content.GetType().ToString() + " to " + typeof(T).ToString() + ".");
                }
            }

            return result;
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

        public static bool CollectionsAreEqual<T>(IEnumerable<T> lhs, IEnumerable<T> rhs) where T : IComparable
        {
            IEnumerator<T> left = lhs.GetEnumerator();
            IEnumerator<T> right = rhs.GetEnumerator();
            bool equal = lhs.Count() == rhs.Count();

            while (equal && left.MoveNext())
            {
                right.MoveNext();
                equal = left.Current.CompareTo(right.Current) == 0;
            }

            return equal;
        }
    }
}
