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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Auremo
{
    public abstract class PlaylistFileParserBase
    {
        protected string m_Input = null;
        protected string m_InputAsLowercase = null;
        protected int m_InputPosition = 0;

        protected PlaylistFileParserBase()
        {
        }

        abstract protected IEnumerable<AudioStream> Parse();

        public IEnumerable<AudioStream> ParseFile(string filename)
        {
            Init(File.ReadAllText(filename));
            return Parse();
        }

        public IEnumerable<AudioStream> ParseString(string input)
        {
            Init(input);
            return Parse();
        }

        private void Init(string input)
        {
            m_Input = input;
            m_InputAsLowercase = m_Input.ToLowerInvariant();
            m_InputPosition = 0;
        }

        protected bool AtEnd
        {
            get
            {
                return m_InputPosition >= m_Input.Length;
            }
        }

        protected char Peek()
        {
            if (AtEnd)
            {
                throw new ParseError();
            }

            return m_Input[m_InputPosition];
        }

        protected char PeekLowercase()
        {
            if (AtEnd)
            {
                throw new ParseError();
            }

            return m_InputAsLowercase[m_InputPosition];
        }

        protected void ConsumeLiteral(string literal)
        {
            if (m_Input.Length < m_InputPosition + literal.Length)
            {
                throw new ParseError();
            }

            foreach (char c in literal.ToLowerInvariant())
            {
                if (PeekLowercase() != c)
                {
                    throw new ParseError();
                }

                m_InputPosition += 1;
            }
        }

        protected string GetRestOfLine()
        {
            int startPosition = m_InputPosition;
            char next = Peek();

            while (!AtEnd && next != '\r' && next != '\n')
            {
                m_InputPosition += 1;
                next = Peek();
            }

            string result = m_Input.Substring(startPosition, m_InputPosition - startPosition);
            ConsumeWhitespace();
            return result;
        }

        protected void ConsumeWhitespace()
        {
            bool whitespace = true;

            while (whitespace && !AtEnd)
            {
                char c = Peek();
                whitespace = c == ' ' || c == '\r' || c == '\n' || c == '\t';

                if (whitespace)
                {
                    m_InputPosition += 1;
                }
            }
        }

        protected class ParseError : Exception
        {
        }
    }
}
