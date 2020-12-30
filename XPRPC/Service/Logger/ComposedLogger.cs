/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;

namespace XPRPC.Service.Logger
{
    public sealed class ComposedLogger : ILogger
    {
        readonly List<ILogger> _innerLoggers = new List<ILogger>();

        public ComposedLogger(List<ILogger> loggers)
        {
            _innerLoggers.AddRange(loggers);
        }

        public void Debug(string tag, string message)
        {
            foreach(var logger in _innerLoggers)
            {
                logger.Debug(tag, message);
            }
        }

        public void Dispose()
        {
            foreach (var logger in _innerLoggers)
            {
                logger.Dispose();
            }
            _innerLoggers.Clear();
        }

        public void Error(string tag, string message)
        {
            foreach (var logger in _innerLoggers)
            {
                logger.Error(tag, message);
            }
        }

        public void Info(string tag, string message)
        {
            foreach (var logger in _innerLoggers)
            {
                logger.Info(tag, message);
            }
        }

        public void Warning(string tag, string message)
        {
            foreach (var logger in _innerLoggers)
            {
                logger.Warning(tag, message);
            }
        }
    }
}
