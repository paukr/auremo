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
    public class DateTemplate
    {
        public DateTemplate(string format)
        {
            ParseFormat(format);
        }

        public string TryToParseDate(string date)
        {
            if (date.Length != m_RequiredInputLength)
            {
                return null;
            }

            Reset();
            m_Date = date;

            foreach (Piece piece in m_ParsedFormat)
            {
                if (!ParseDatePiece(piece))
                {
                    return null;
                }
            }

            if (m_InputPosition == date.Length)
            {
                return m_ResultYear + '-' + m_ResultMonth + '-' + m_ResultDay + '-' + m_ResultNumber;
            }

            return null;
        }

        private enum PieceKind
        {
            Year,
            Month,
            Day,
            Number,
            Literal
        };

        struct Piece
        {
            public PieceKind Kind;
            public int Length;
            public string LiteralCharacters;

            public Piece(PieceKind kind)
            {
                Kind = kind;
                Length = 0;
                LiteralCharacters = "";
            }
        };

        private List<Piece> m_ParsedFormat = null;
        private string m_Date = null;
        private int m_RequiredInputLength = 0;
        private int m_InputPosition = 0;
        private string m_ResultYear = "";
        private string m_ResultMonth = "";
        private string m_ResultDay = "";
        private string m_ResultNumber = "";

        private void Reset()
        {
            m_Date = null;
            m_InputPosition = 0;
            m_ResultYear = "0000";
            m_ResultMonth = "01";
            m_ResultDay = "01";
            m_ResultNumber = "000000";
        }

        private void ParseFormat(string format)
        {
            m_ParsedFormat = new List<Piece>();
            m_RequiredInputLength = 0;
            int i = 0;

            while (i < format.Length)
            {
                Piece piece;

                if (format[i] == 'Y' || format[i] == 'y')
                {
                    piece = new Piece(PieceKind.Year);

                    while (i < format.Length && (format[i] == 'Y' || format[i] == 'y'))
                    {
                        piece.Length += 1;
                        i += 1;
                    }
                }
                else if (format[i] == 'M' || format[i] == 'm')
                {
                    piece = new Piece(PieceKind.Month);

                    while (i < format.Length && (format[i] == 'M' || format[i] == 'm'))
                    {
                        piece.Length += 1;
                        i += 1;
                    }
                }
                else if (format[i] == 'D' || format[i] == 'd')
                {
                    piece = new Piece(PieceKind.Day);

                    while (i < format.Length && (format[i] == 'D' || format[i] == 'd'))
                    {
                        piece.Length += 1;
                        i += 1;
                    }
                }
                else if (format[i] == 'N' || format[i] == 'n')
                {
                    piece = new Piece(PieceKind.Number);

                    while (i < format.Length && (format[i] == 'N' || format[i] == 'n'))
                    {
                        piece.Length += 1;
                        i += 1;
                    }
                }
                else
                {
                    piece = new Piece(PieceKind.Literal);

                    while (i < format.Length && IsLiteralFormattingCharacter(format[i]))
                    {
                        piece.LiteralCharacters += format[i];
                        piece.Length += 1;
                        i += 1;
                    }
                }

                m_ParsedFormat.Add(piece);
                m_RequiredInputLength += piece.Length;
            }
        }

        private bool ParseDatePiece(Piece piece)
        {
            if (piece.Kind == PieceKind.Literal)
            {
                for (int i = 0; i < piece.Length; ++i)
                {
                    if (m_Date[m_InputPosition] != piece.LiteralCharacters[i])
                    {
                        return false;
                    }

                    m_InputPosition += 1;
                }
            }
            else
            {
                string parsedNumber;

                if (piece.Kind == PieceKind.Year && piece.Length == 2)
                {
                    // Assume "YY" means 19XX.
                    parsedNumber = "19";
                }
                else
                {
                    parsedNumber = "000000";
                }

                for (int i = 0; i < piece.Length; ++i)
                {
                    if (!Char.IsDigit(m_Date[m_InputPosition]))
                    {
                        return false;
                    }

                    parsedNumber += m_Date[m_InputPosition];
                    m_InputPosition += 1;
                }

                if (piece.Kind == PieceKind.Year)
                {
                    m_ResultYear = parsedNumber.Substring(parsedNumber.Length - 4);
                }
                else if (piece.Kind == PieceKind.Month)
                {
                    m_ResultMonth = parsedNumber.Substring(parsedNumber.Length - 2);
                }
                else if (piece.Kind == PieceKind.Day)
                {
                    m_ResultDay = parsedNumber.Substring(parsedNumber.Length - 2);
                }
                else if (piece.Kind == PieceKind.Number)
                {
                    m_ResultNumber = parsedNumber.Substring(parsedNumber.Length - 6);
                }
            }

            return true;
        }

        private bool IsLiteralFormattingCharacter(char c)
        {
            return c != 'Y' && c != 'y' && c != 'M' && c != 'm' && c != 'D' && c != 'd' && c != 'N' && c != 'n';
        }
    }
}
