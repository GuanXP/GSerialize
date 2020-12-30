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
    public abstract class ServiceRunner<TService> : IDisposable
        where TService : IDisposable
    {
        protected abstract IServiceManager ResolveServiceManager(ServiceDescriptor descriptor);

        public string ServiceName => Descriptor.Name;
        private bool _started = false;
        private readonly bool _holdService;
        private readonly Timer _timerPing;

        public ServiceRunner(TService service, ServiceDescriptor descriptor, bool holdService)
        {
            Service = service;
            Descriptor = descriptor;
            _holdService = holdService;
            Descriptor.AccessToken = TokenGenerator.RandomToken(64);
            Descriptor.InterfaceName = typeof(TService).FullName;
            _timerPing = new Timer(
                callback: OnTimerPing, 
                state: null, 
                dueTime: Timeout.Infinite, 
                period: 10_000);
        }

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
                    throw new SecurityException($"{_clientID} failed authorization");
                }
            }

            if (!ServiceManager.ServiceExists(ServiceName))
            {
                _serviceToken = ServiceManager.AddService(Descriptor);
                if (string.IsNullOrEmpty(_serviceToken))
                {
                    throw new SecurityException($"service {ServiceName} failed to publish, please confirm access.");
                }
                System.Diagnostics.Debug.WriteLine($"service {ServiceName} published");
            }
            else
            {
                throw new SecurityException($"service {ServiceName} failed to publish, duplicate service names");
            }
        }

        private void ResignService()
        {
            if (ServiceManager != null)
            {
                if (ServiceManager.ServiceExists(ServiceName) && !string.IsNullOrEmpty(_serviceToken))
                {
                    System.Diagnostics.Debug.WriteLine($"service {ServiceName} resigned");
                    ServiceManager.RemoveService(Descriptor.Name, _serviceToken);
                }
                ServiceManager.Dispose();
                ServiceManager = null;
            }
        }

        public void Stop()
        {
            if (_started)
            {
                _started = false;
                ResignService();
                _timerPing.Change(dueTime: Timeout.InfiniteTimeSpan, period: Timeout.InfiniteTimeSpan);
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
                    Stop();
                    _timerPing.Dispose();
                    if (_holdService)
                    {
                        Service.Dispose();
                    }                   
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
