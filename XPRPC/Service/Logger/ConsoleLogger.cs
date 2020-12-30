/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
using System;

namespace XPRPC.Service.Logger
{
    public sealed class ConsoleLogger : ILogger
    {
        public void Debug(string tag, string message)
        {
            Console.WriteLine($"D {tag}:{message}");
        }

        public void Dispose()
        {
            //Do nothing
        }

        public void Error(string tag, string message)
        {
            Console.WriteLine($"E {tag}:{message}");
        }

        public void Info(string tag, string message)
        {
            Console.WriteLine($"I {tag}:{message}");
        }

        public void Warning(string tag, string message)
        {
            Console.WriteLine($"W {tag}:{message}");
        }
    }
}
