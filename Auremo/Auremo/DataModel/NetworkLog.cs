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

namespace Auremo
{
    public class NetworkLog
    {
        private TextWriter m_Log = null;
        private bool m_Verbose = false;

        public NetworkLog(string filename, bool verbose)
        {
            m_Verbose = verbose;
            FileStream file = File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.Read);
            m_Log = new StreamWriter(file);
            LogMessage("Logging started");
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

        public void LogResponseCompact(MPDResponseLine response)
        {
            if (!m_Verbose)
            {
                Write("R: " + response.ToString());
            }
        }

        public void LogResponseVerbose(MPDResponseLine response)
        {
            if (m_Verbose)
            {
                Write("R: " + response.ToString());
            }
        }

        public void LogMessage(string message)
        {
            Write("--- " + message + " ---");
        }

        private void Write(string s)
        {
            string message = GetTimestampPrefix() + " " + s;

            lock (this)
            {
                m_Log.WriteLine(message);
                m_Log.Flush();
            }
        }

        private string GetTimestampPrefix()
        {
            return DateTime.Now.ToString(@"HH\:mm\:ss.fff");
        }
    }
}
