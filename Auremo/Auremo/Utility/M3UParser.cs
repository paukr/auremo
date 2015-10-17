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

using Auremo.MusicLibrary;
using System.Collections.Generic;

namespace Auremo
{
    public class M3UParser : PlaylistFileParserBase
    {
        private IList<AudioStream> m_ParsedStreams = null;
        private bool m_ExtendedFormat = false;
        
        public M3UParser()
        {
        }

        protected override IEnumerable<AudioStream> Parse()
        {
            m_ParsedStreams = new List<AudioStream>();

            try
            {
                ParseHeader();

                while (m_InputPosition < m_Input.Length)
                {
                    ParseEntry();
                }
            }
            catch (ParseError)
            {
                m_ParsedStreams = null;
            }

            IEnumerable<AudioStream> result = m_ParsedStreams;
            m_ParsedStreams = null;
            
            return result;
        }

        private void ParseHeader()
        {
            m_ExtendedFormat = false;
            ConsumeWhitespace();

            if (m_InputPosition < m_Input.Length && m_Input[m_InputPosition] == '#')
            {
                ConsumeLiteral("#EXTM3U");
                ConsumeWhitespace();
                m_ExtendedFormat = true;
            }
        }

        private void ParseEntry()
        {
            string path = null;
            string label = null;

            if (m_ExtendedFormat && Peek() == '#')
            {
                ConsumeLiteral("#EXTINF:");
                IgnoreUntil(',');
                ConsumeLiteral(",");
                label = GetRestOfLine();
                ConsumeWhitespace();
            }

            path = GetRestOfLine();
            ConsumeWhitespace();

            if (path != "")
            {
                m_ParsedStreams.Add(new AudioStream(new Path(path), label));
            }
        }

        private void IgnoreUntil(char c)
        {
            while (Peek() != c)
            {
                m_InputPosition += 1;
            }
        }
    }
}
