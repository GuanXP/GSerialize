/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
using System.Security.Cryptography.X509Certificates;
using XPRPC.Service.Logger;

namespace XPRPC.Server
{
    class ServiceManagerTcpServer : TcpServer<IServiceManager>
    {
        private readonly ServiceManager _manager;
        internal ServiceManagerTcpServer(
            ServiceDescriptor descriptor, 
            ServiceManager service,
            ILogger logger, 
            X509Certificate sslCertificate): base(null, descriptor, logger, sslCertificate)
        {
            _manager = service;
        }

        protected override IServiceManager SessionService
        {
            get
            {
                return new ServiceManagerSession(_manager);
            }
        }
    }
}