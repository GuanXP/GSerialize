/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
using System;
using System.Collections.Generic;
using System.Security;

namespace XPRPC.Client
{
    public abstract class ServiceResolver : IDisposable
    {
        protected abstract IServiceManager ResolveServiceManager(ServiceDescriptor descriptor);
        protected abstract TService ResolveService<TService>(ServiceDescriptor descriptor) where TService: IDisposable;

        protected ServiceResolver(ServiceDescriptor serviceManagerDesc, string clientID, string secretKey)
        {
            ClientID = clientID;
            SecretKey = secretKey;

            var manager = ResolveServiceManager(serviceManagerDesc);
            if (!manager.AuthenticateClient(clientID, secretKey))
            {
                manager.Dispose();
                throw new SecurityException("Failed to authenticate the client");
            }
            _serviceManager = manager;
        }

        public TService GetService<TService>(string name) where TService: IDisposable
        {
            IDisposable service = null;
            lock(_lock)
            {
                _indexedServices.TryGetValue(name, out service);
            }
            if (service == null || !ServiceIsActive(service))
            {
                var desc = _serviceManager.GetService(name);
                var newService = ResolveService<TService>(desc);
                lock (_lock)
                {
                    _indexedServices.TryGetValue(name, out service);
                    if (service == null && newService != null)
                    {
                        service?.Dispose();
                        _indexedServices[name] = newService;
                        _services.Insert(0, newService);  //place the newest service at head
                        return newService;
                    }
                }
                newService?.Dispose();                
            }
            return (TService)service;
        }

        protected abstract bool ServiceIsActive(IDisposable service);

        private IServiceManager _serviceManager;
        public IServiceManager ServiceManager => _serviceManager;
        private Dictionary<string, IDisposable> _indexedServices = new Dictionary<string, IDisposable>();
        private List<IDisposable> _services = new List<IDisposable>();
        private object _lock = new object();

        protected string ClientID{get; private set;}
        protected string SecretKey{get; private set;}

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    DisposeServices();
                    _serviceManager.Dispose();                    
                }
                _serviceManager = null;
                disposedValue = true;
            }
        }

        private void DisposeServices()
        {
            var services = new List<IDisposable>();
            lock(_lock)
            {                
                services.AddRange(_services);                
                _services.Clear();
                _indexedServices.Clear();
            }

            //Be careful about the disposing sequence that are inverse to creating order
            foreach(var service in services)
            {
                service.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
