/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
using System;
using System.IO;
using System.Threading.Tasks;

namespace XPRPC
{
    public class ProxyItem : IDisposable
    {
        public Type InterfaceType{ get; private set; }
        public Int16 RemoteObjectID{ get; private set; }
        public IProxy Proxy{ get; private set; }
        private readonly IDataSender _sender;
        private bool disposedValue;

        private ProxyItem(Type interfaceType, Int16 id, IDataSender sender)
        {
            InterfaceType = interfaceType;
            RemoteObjectID = id;
            _sender = sender;
            Proxy = ProxyGenerator.Instance.CreateProxy(this);
        }

        public static ProxyItem Build<TService>(Int16 remoteObjectID, IDataSender sender)
        {
            System.Diagnostics.Debug.Assert(typeof(TService).IsInterface);
            var proxy = new ProxyItem(typeof(TService), remoteObjectID, sender);
            System.Diagnostics.Debug.Assert(typeof(TService).IsAssignableFrom(proxy.Proxy.GetType()));
            return proxy;
        }

        public MemoryStream SendCallRequest(Int16 methodID, byte[] requestData)
        {
            return _sender.SendCallRequest(RemoteObjectID, methodID, requestData);
        }

        public Task<MemoryStream> SendCallRequestAsync(Int16 methodID, byte[] requestData)
        {
            return _sender.SendCallRequestAsync(RemoteObjectID, methodID, requestData);
        }
        public Int16 CacheService<TService>(TService service)
        {
            return _sender.CacheService(service);
        }
        public TService CacheProxy<TService>(Int16 remoteObjectID)
        {
            return _sender.CacheProxy<TService>(remoteObjectID);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Proxy?.Dispose();
                    Proxy = null;
                }

                disposedValue = true;
            }
        }


        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}