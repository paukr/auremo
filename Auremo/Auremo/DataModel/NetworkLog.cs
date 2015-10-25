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
 
using System.IO;
using System.Text;

namespace Auremo
{
    public class NetworkLog
    {
        StringBuilder m_Log = new StringBuilder();

        public NetworkLog()
        {
        }

        public void LogCommand(string command)
        {
            lock (this)
            {
                m_Log.AppendLine("S: " + command);
            }
        }

        public void LogResponse(MPDResponseLine response)
        {
            lock (this)
            {
                m_Log.AppendLine("R: " + response.ToString());
            }
        }

        public void LogMessage(string message)
        {
            lock (this)
            {
                m_Log.AppendLine("--- " + message + " ---");
            }
        }

        public void WriteToFile(string filename)
        {
            lock (this)
            {
                File.WriteAllText(filename, m_Log.ToString());
            }
        }
    }
}
