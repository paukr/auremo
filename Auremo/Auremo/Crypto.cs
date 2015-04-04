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
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace Auremo
{
    // Provide safe password storage.
    //
    // CAUTION: Aspects of this are not in fact safe. A plaintext version of the
    // password will reside in memory after it has been saved to disk of sent to
    // the server. However, since the MPD protocol requires that the password is
    // sent in plaintext form, the password is easily retrieved by spying on the
    // network. If this changes, this code must be rewritten, but this should be
    // enough for now. If on the other hand you are looking for examples of good
    // password storing code for another project, *DO* *NOT* use code from here.
    // You should also notice the neat justified right column in this paragraph.

    public class Crypto
    {
        private static byte[] m_Salt =
        {
            0xBA, 0x4E, 0x30, 0x0E,
            0xAC, 0x44, 0x7C, 0x8C,
            0x52, 0x9B, 0xB5, 0x22,
            0x20, 0xF3, 0x42, 0x76 
        };                                   

        public static string EncryptPassword(string plainText)
        {
            if (plainText.Length > 0)
            {
                try
                {
                    byte[] plainBytes = Encoding.Unicode.GetBytes(plainText);
                    byte[] cipherText = ProtectedData.Protect(plainBytes, m_Salt, DataProtectionScope.CurrentUser);
                    return Convert.ToBase64String(cipherText);
                }
                catch (Exception)
                {
                }
            }

            return "";
        }

        public static string DecryptPassword(string cipherText)
        {
            if (cipherText.Length > 0)
            {
                try
                {
                    byte[] cipherBytes = Convert.FromBase64String(cipherText);
                    byte[] plainBytes = ProtectedData.Unprotect(cipherBytes, m_Salt, DataProtectionScope.CurrentUser);
                    return Encoding.Unicode.GetString(plainBytes);
                }
                catch (Exception)
                {
                }
            }

            return "";
        }
    }
}
