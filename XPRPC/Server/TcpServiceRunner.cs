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
    /// <summary>
    /// Run service via TCP protocol
    /// </summary>
    public class TcpServiceRunner<TService> : ServiceRunner<TService>
    {
        TcpServer<TService> _tcpServer;
        Client.TcpServiceResolver _resolver;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="service">the service object</param>        
        /// <param name="descriptor">service descriptor</param>        
        /// <param name="logger">reference to ILogger. TcpServiceRunner doesn't dispose the logger</param>
        /// <param name="sslCertificate">The certificate to build SSL, null means no SSL used. </param>
        public TcpServiceRunner(
            TService service,             
            ServiceDescriptor descriptor, 
            ILogger logger,
            X509Certificate sslCertificate)
        : base(service, descriptor)
        {            
            if (sslCertificate != null)
            {
                descriptor.UseSSL = true;
            }
            _tcpServer = new TcpServer<TService>(service, descriptor, logger, sslCertificate);
        }

        protected override void Dispose(bool disposing)
        {
            //Sequence restrict: before closing ServiceManager, to resign published service first
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