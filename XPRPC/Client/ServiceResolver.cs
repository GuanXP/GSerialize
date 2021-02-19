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
    /// <summary>
    /// Base class to resolve a service
    /// </summary>
    public abstract class ServiceResolver : IDisposable
    {
        protected abstract IServiceManager ResolveServiceManager(ServiceDescriptor descriptor);
        protected abstract TService ResolveService<TService>(ServiceDescriptor descriptor);

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="serviceManagerDesc">Descriptor to resolve the service manager</param>
        /// <param name="clientID">Client ID to access the service</param>
        /// <param name="secretKey">Secret key for the client ID</param>
        protected ServiceResolver(ServiceDescriptor serviceManagerDesc, string clientID, string secretKey)
        {
            ClientID = clientID;
            SecretKey = secretKey;

            var manager = ResolveServiceManager(serviceManagerDesc);
            if (!manager.AuthenticateClient(clientID, secretKey))
            {
                manager.Dispose();
                throw new SecurityException("Invalid client ID or secret key");
            }
            _serviceManager = manager;
        }

        /// <summary>
        /// Get a service object from the service manager
        /// </summary>
        /// <param name="name">The service name</param>
        public TService GetService<TService>(string name)
        {
            Object service = null;
            lock(_lock)
            {
                _indexedServices.TryGetValue(name, out service);
            }
            if (service == null || !ServiceIsActive(service))
            {
                var desc = _serviceManager.GetService(name);
                if (!desc.IsValid) return default(TService);

                var newService = ResolveService<TService>(desc);
                lock (_lock)
                {
                    _indexedServices.TryGetValue(name, out service);
                    if (service == null && newService != null)
                    {
                        _indexedServices[name] = newService;
                        return newService;
                    }
                }
            }
            return (TService)service;
        }

        protected abstract bool ServiceIsActive(Object service);

        private IServiceManager _serviceManager;
        public IServiceManager ServiceManager => _serviceManager;
        private Dictionary<string, Object> _indexedServices = new Dictionary<string, Object>();
        private readonly object _lock = new object();

        protected string ClientID{get; private set;}
        protected string SecretKey{get; private set;}

        #region IDisposable

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _serviceManager?.Dispose();
                    _serviceManager = null;
                }
                
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable
    }
}
