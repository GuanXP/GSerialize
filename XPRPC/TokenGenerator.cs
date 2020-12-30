/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
using System;
using System.Text;

namespace XPRPC
{
    public sealed class TokenGenerator
    {
        static readonly char[] CHARS = "1234567890abcdfeghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVQXYZ".ToCharArray();
        static readonly Random RANDOM = new Random();
        public static String RandomToken(int length)
        {
            var b = new StringBuilder(length);
            for (var i = 0; i < length; ++i)
            {
                b.Append(CHARS[RANDOM.Next(CHARS.Length)]);
            }
            return b.ToString();
        }
    }
}
