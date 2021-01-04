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

    public sealed class TcpServiceResolver : ServiceResolver
    {
        readonly LinkedList<ConnectionRecord> _connections = new LinkedList<ConnectionRecord>();
        readonly object _lock = new object();

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
                    _connections.AddFirst(record);
                }
                connection = record.Connection as TcpConnection<TService>;
            }
            connection.Connect();
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
            base.Dispose(disposing);
            CloseConnections();
        }

        private void CloseConnections()
        {
            foreach(var c in _connections)
            {
                c.Connection.Dispose();
            }
            _connections.Clear();
        }
    }
}