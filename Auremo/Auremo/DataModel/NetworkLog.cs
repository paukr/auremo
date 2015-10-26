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
using System.Text;

namespace Auremo
{
    public class NetworkLog
    {
        private string m_Filename = null;

        public NetworkLog(string filename)
        {
            m_Filename = filename;
            File.WriteAllText(m_Filename, "--- Auremo diagnostics log ---" + Environment.NewLine);
        }

        public void LogCommand(string command)
        {
            if (command.ToLowerInvariant().StartsWith("password"))
            {
                Write("S: password: <redacted>");
            }
            else
            {
                Write("S: " + command);
            }
        }

        public void LogResponse(MPDResponseLine response)
        {
             Write("R: " + response.ToString());
        }

        public void LogMessage(string message)
        {
            Write("--- " + message + " ---");
        }

        private void Write(string s)
        {
            lock (this)
            {
                File.AppendAllText(m_Filename, s + Environment.NewLine);
            }
        }
    }
}
