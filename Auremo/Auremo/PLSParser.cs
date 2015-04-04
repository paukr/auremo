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
using System.IO;
using System.Linq;
using System.Text;

namespace Auremo
{
    public class PLSParser : PlaylistFileParserBase
    {
        private IDictionary<int, StreamMetadata> m_ParsedStreams = null;

        public PLSParser()
        {
        }

        protected override IEnumerable<StreamMetadata> Parse()
        {
            m_ParsedStreams = new SortedDictionary<int, StreamMetadata>();
            IList<StreamMetadata> result = null;

            try
            {
                ParseHeader();
                ParseEntries();

                result = new List<StreamMetadata>();

                foreach (StreamMetadata stream in m_ParsedStreams.Values)
                {
                    if (stream.Path != null)
                    {
                        result.Add(stream);
                    }
                }
            }
            catch (ParseError)
            {
                result = null;
            }

            m_ParsedStreams = null;
            return result;
        }

        private void ParseHeader()
        {
            ConsumeLiteral("[playlist]");
            ConsumeWhitespace();
        }

        private void ParseEntries()
        {
            while (!AtEnd)
            {
                string key = GetKey();
                string value = GetRestOfLine();

                if (key == "version")
                {
                    if (value == "2")
                    {
                        return;
                    }
                    else
                    {
                        throw new ParseError();
                    }
                }
                else
                {
                    if (key.StartsWith("length") || key.StartsWith("numberofentries"))
                    {
                        continue;
                    }

                    int index = GetNumberSuffix(key);

                    if (!m_ParsedStreams.ContainsKey(index))
                    {
                        m_ParsedStreams.Add(index, new StreamMetadata(null, null));
                    }

                    if (key.StartsWith("file"))
                    {
                        m_ParsedStreams[index].Path = value;
                    }
                    else if (key.StartsWith("title"))
                    {
                        m_ParsedStreams[index].Label = value;
                    }
                    else
                    {
                        throw new ParseError();
                    }
                }
            }
        }
    
        private void ParseVersion()
        {
            bool versionFound = false;

            do
            {
                string key = GetKey();
                string value = GetRestOfLine();

                if (key == "version")
                {
                    versionFound = value == "2";

                    if (!versionFound)
                    {
                        throw new ParseError();
                    }
                }
            } while (!versionFound);
        }
        
        private string GetKey()
        {
            int startPosition = m_InputPosition;

            while (!AtEnd && PeekLowercase() != '=')
            {
                m_InputPosition += 1;
            }

            string result = m_InputAsLowercase.Substring(startPosition, m_InputPosition - startPosition);
            ConsumeLiteral("=");
            
            return result;
        }

        private int GetNumberSuffix(string from)
        {
            int start = 0;

            while (start < from.Length && !Char.IsDigit(from[start]))
            {
                start += 1;
            }

            if (start >= from.Length)
            {
                throw new ParseError();
            }

            string suffix = from.Substring(start);
            int? result = Utils.StringToInt(suffix);

            if (result == null)
            {
                throw new ParseError();
            }

            return result.Value;
        }
    }
}
