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
    public sealed class LoggerBuilder
    {
        public bool ConsoleEnabled { get; set; } = true;
        public bool DebugEnabled { get; set; } = true;
        public string LogFile { get; set; }

        private List<ILogger> _loggers = new List<ILogger>();

        public LoggerBuilder Add(ILogger logger)
        {
            _loggers.Add(logger);
            return this;
        }

        public ILogger Build()
        {
            var loggers = new List<ILogger>();
            if (ConsoleEnabled)
            {
                loggers.Add(new ConsoleLogger());
            }
            if (DebugEnabled)
            {
                loggers.Add(new DebugLogger());
            }
            if (!string.IsNullOrEmpty(LogFile))
            {
                loggers.Add(new FileLogger(LogFile));
            }

            loggers.AddRange(_loggers);
            _loggers.Clear();

            if (loggers.Count == 0)
            {
                loggers.Add(new BlackHoleLogger());
            }
            return new ComposedLogger(loggers);
        }
    }
}
