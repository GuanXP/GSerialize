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
    public class TcpServiceRunner<TService> : ServiceRunner<TService>
    where TService : IDisposable
    {
        TcpServer<TService> _tcpServer;
        Client.TcpServiceResolver _resolver;

        public TcpServiceRunner(
            TService service,             
            ServiceDescriptor descriptor, 
            bool holdService,
            ILogger logger,
            X509Certificate sslCertificate)
        : base(service, descriptor, holdService)
        {            
            if (sslCertificate != null)
            {
                descriptor.UseSSL = true;
            }
            _tcpServer = new TcpServer<TService>(service, descriptor, logger, sslCertificate);
        }

        protected override void Dispose(bool disposing)
        {
            // be careful about the sequence, resign service from ServiceManager and dispose ServiceManager
            base.Dispose(disposing);
            _tcpServer?.Dispose();
            _tcpServer = null;
            _resolver?.Dispose();
            _resolver = null;
        }

        protected override IServiceManager ResolveServiceManager(ServiceDescriptor descriptor)
        {
            if (_resolver == null)
            {
                _resolver = new Client.TcpServiceResolver(descriptor, ClientID, SecretKey);
            }
            return _resolver.ServiceManager;
        }
    }
}