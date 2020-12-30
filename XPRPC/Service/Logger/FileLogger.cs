/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace XPRPC.Service.Logger
{
    public sealed class FileLogger : ILogger
    {
        private bool disposedValue;
        private readonly object _lock = new object();
        private readonly StreamWriter _writer;
        private LinkedList<string> _messageQ = new LinkedList<string>();
        private bool _writting = false;

        public FileLogger(string logFile)
        {
            _writer = new StreamWriter(new FileStream(logFile, FileMode.Append));
        }

        public void Debug(string tag, string message)
        {
            WriteLine($"D {tag}:{message}");
        }

        public void Error(string tag, string message)
        {
            WriteLine($"E {tag}:{message}");
        }

        public void Info(string tag, string message)
        {
            WriteLine($"I {tag}:{message}");
        }

        public void Warning(string tag, string message)
        {
            WriteLine($"W {tag}:{message}");
        }

        private void WriteLine(string message)
        {
            lock (_lock)
            {
                _messageQ.AddLast($"{DateTime.Now} {message}");
                if (!_writting)
                {
                    _writting = true;
                    Task.Run(WriteQToFile);
                }
            }
        }

        private void WriteQToFile()
        {            
            LinkedList<string> q;
            while (true)
            {
                lock (_lock)
                {
                    if (_messageQ.Count == 0)
                    {
                        _writting = false;
                        Monitor.PulseAll(_lock);
                        break;
                    }
                    q = _messageQ;
                    _messageQ = new LinkedList<string>();
                }

                WriteOut(q);
            }
        }

        private void WriteOut(LinkedList<string> q)
        {
            foreach (var message in q)
            {
                _writer.WriteLine(message);
            }
            _writer.Flush();
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    WaitAllMessageWritten();
                    _writer.Close();
                }
                disposedValue = true;
            }
        }

        private void WaitAllMessageWritten()
        {
            lock (_lock)
            {
                while (_messageQ.Count > 0)
                {
                    Monitor.Wait(_lock);
                }
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }
    }
}
