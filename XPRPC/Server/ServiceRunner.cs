/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
using System;
using System.Security;
using System.Threading;

namespace XPRPC.Server
{
    /// <summary>
    /// Runner to publish a service
    /// </summary>
    public abstract class ServiceRunner<TService> : IDisposable
    {
        /// <summary>
        /// Resolve IServiceManager
        /// </summary>
        /// <param name="descriptor">service descriptor of IServiceManager</param>
        /// <returns>Reference to IServiceManager, ServiceRunner will dispose it</returns>
        protected abstract IServiceManager ResolveServiceManager(ServiceDescriptor descriptor);

        public string ServiceName => Descriptor.Name;
        private bool _started = false;
        private readonly Timer _timerPing;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="service">The service entity</param>
        /// <param name="descriptor">service descriptor</param>
        public ServiceRunner(TService service, ServiceDescriptor descriptor)
        {
            Service = service;
            Descriptor = descriptor;
            Descriptor.AccessToken = TokenGenerator.RandomToken(64);
            Descriptor.InterfaceName = typeof(TService).FullName;
            _timerPing = new Timer(
                callback: OnTimerPing, 
                state: null, 
                dueTime: Timeout.Infinite, 
                period: 10_000);
        }

        /// <summary>
        /// Start the runner
        /// </summary>
        /// <param name="serviceManagerDescriptor">service descriptor of IServiceManager</param>
        /// <param name="clientID">client ID to publish this service </param>
        /// <param name="secretKey">secret key of the publishing client ID</param>
        public void Start(ServiceDescriptor serviceManagerDescriptor, string clientID, string secretKey)
        {
            if (_started) return;
            _started = true;

            _managerDescriptor = serviceManagerDescriptor;
            _clientID = clientID;
            _secretKey = secretKey;

            PublishService();
            _timerPing.Change(dueTime: 0, period: 10_000);            
        }

        private void OnTimerPing(Object state)
        {
            ServiceManager?.ReportServiceHealthy(Descriptor.Name, _serviceToken);
        }

        private void PublishService()
        {
            if (ServiceManager == null)
            {
                ServiceManager = ResolveServiceManager(_managerDescriptor);
                if (!ServiceManager.AuthenticateClient(_clientID, _secretKey))
                {
                    throw new SecurityException($"{_clientID} fail to authenticate.");
                }
            }

            if (!ServiceManager.ServiceExists(ServiceName))
            {
                _serviceToken = ServiceManager.AddService(Descriptor);
                if (string.IsNullOrEmpty(_serviceToken))
                {
                    throw new SecurityException($"{ServiceName} fail to publish, access right required.");
                }
                System.Diagnostics.Debug.WriteLine($"succeed publishing {ServiceName}");
            }
            else
            {
                throw new SecurityException($"{ServiceName} fail to authenticate, service already exists");
            }
        }

        private void ResignService()
        {
            if (ServiceManager != null)
            {
                if (ServiceManager.ServiceExists(ServiceName) && !string.IsNullOrEmpty(_serviceToken))
                {
                    System.Diagnostics.Debug.WriteLine($"resign service {ServiceName}");
                    ServiceManager.RemoveService(Descriptor.Name, _serviceToken);
                }
                ServiceManager.Dispose();
                ServiceManager = null;
            }
        }

        private void Stop()
        {
            if (_started)
            {
                _started = false;
                ResignService();
            }
        }

        protected TService Service{get; private set;}
        protected ServiceDescriptor Descriptor{get; private set;}
        protected IServiceManager ServiceManager{get; private set;}

        private ServiceDescriptor _managerDescriptor;
        private string _clientID, _secretKey, _serviceToken;
        protected string ClientID => _clientID;
        protected string SecretKey => _secretKey;

        #region IDisposable
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _timerPing.Dispose();
                    Stop();                    
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
