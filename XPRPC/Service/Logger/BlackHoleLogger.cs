/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
namespace XPRPC.Service.Logger
{
    sealed class BlackHoleLogger : ILogger
    {
        public void Debug(string tag, string message)
        {
        }

        public void Dispose()
        {
        }

        public void Error(string tag, string message)
        {
        }

        public void Info(string tag, string message)
        {
        }

        public void Warning(string tag, string message)
        {
        }
    }
}
