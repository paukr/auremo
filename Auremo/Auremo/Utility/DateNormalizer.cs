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

using System.Collections.Generic;
using System.Collections.Specialized;
using Auremo.Properties;

namespace Auremo
{
    public class DateNormalizer
    {
        List<DateTemplate> m_Templates = new List<DateTemplate>();

        public DateNormalizer()
        {
        }

        public DateNormalizer(IEnumerable<string> formats)
        {
            SetFormats(formats);
        }

        public void SetFormats(IEnumerable<string> formats)
        {
            m_Templates.Clear();

            foreach (string format in formats)
            {
                m_Templates.Add(new DateTemplate(format));
            }
        }

        public void ReadFromSettings()
        {
            StringCollection formatCollection = Settings.Default.AlbumDateFormats;
            IList<string> formatList = new List<string>();

            foreach (string format in formatCollection)
            {
                formatList.Add(format);
            }

            SetFormats(formatList);
        }

        public string Normalize(string date)
        {
            if (date != null)
            {
                foreach (DateTemplate template in m_Templates)
                {
                    string result = template.TryToParseDate(date);

                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            return null;
        }
    }
}
