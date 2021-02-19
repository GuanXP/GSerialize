/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace XPRPC.Client
{
    class ConnectionRecord
    {
        public IDisposable Connection;
        public Type InterfaceType;
        public ServiceDescriptor Descriptor;
    }
    /// <summary>
    /// Class to resolve remote service via TCP connection.
    /// </summary>
    public sealed class TcpServiceResolver : ServiceResolver
    {
        readonly LinkedList<ConnectionRecord> _connections = new LinkedList<ConnectionRecord>();
        readonly object _lock = new object();
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="serviceManagerDesc">Descriptor to resolve the service manager</param>
        /// <param name="clientID">Client ID to access the service</param>
        /// <param name="secretKey">Secret key for the client ID</param>
        public TcpServiceResolver(ServiceDescriptor serviceManagerDesc, string clientID, string secretKey)
            : base(serviceManagerDesc, clientID, secretKey)
        {
        }

        private ConnectionRecord FindRecord(ServiceDescriptor descriptor, Type interfaceType)
        {
            return (from c in _connections
                    where c.InterfaceType == interfaceType
                    && c.Descriptor.IsIdentityWith(descriptor)
                    select c).FirstOrDefault();
        }

        protected override TService ResolveService<TService>(ServiceDescriptor descriptor)
        {
            TcpConnection<TService> connection;
            lock(_lock)
            {
                var record = FindRecord(descriptor, typeof(TService));
                if (record == null)
                {
                    record = new ConnectionRecord
                    {
                        Connection = new TcpConnection<TService>(descriptor),
                        InterfaceType = typeof(TService),
                        Descriptor = descriptor
                    };
                    _connections.AddFirst(record); // Add head to close first
                }
                connection = record.Connection as TcpConnection<TService>;
            }

            return connection.GetService();
        }

        protected override IServiceManager ResolveServiceManager(ServiceDescriptor descriptor)
        {
            return ResolveService<IServiceManager>(descriptor);
        }

        protected override bool ServiceIsActive(Object service)
        {
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            // We need dispose the service proxies before close the connection
            base.Dispose(disposing);
            CloseConnections();
        }

        private void CloseConnections()
        {
            // Inverse order to dispose since there may be dependencies among the proxies
            foreach(var c in _connections)
            {
                c.Connection.Dispose();
            }
            _connections.Clear();
        }
    }
}