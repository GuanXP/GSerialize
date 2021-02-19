/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
using System;
using System.Security.Cryptography.X509Certificates;
using XPRPC.Service.Logger;

namespace XPRPC.Server
{
    public class TcpManagerRunner : ManagerRunner, IDisposable
    {
        private bool disposedValue;
        private ServiceManagerTcpServer _tcpServer;
        public ILogger Logger { get; set;}

        public static readonly TcpManagerRunner Instance = new TcpManagerRunner();

        public override void Start(ServiceDescriptor descriptor, X509Certificate sslCertificate)
        {
            if (_tcpServer == null)
            {
                _tcpServer = new ServiceManagerTcpServer(descriptor, Service, Logger, sslCertificate);
                StartCheckHealthy();
            }
        }

        public override void Stop()
        {
            _tcpServer?.Dispose();
            _tcpServer = null;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Stop();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}