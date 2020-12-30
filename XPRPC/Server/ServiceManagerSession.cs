/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
using System;
using System.Collections.Generic;

namespace XPRPC.Server
{
    class ServiceManagerSession : IServiceManager
    {
        string _clientID;
        private bool disposedValue;
        readonly ServiceManager _manager;

        public event EventHandler<ServiceDeadEventArgs> ServiceDeadEvent;

        private bool Authenticated => !string.IsNullOrEmpty(_clientID);

        public ServiceManagerSession(ServiceManager manager)
        {
            _manager = manager;
            _manager.ServiceDeadEvent += ServiceDeadEvent;
        }

        public bool AuthenticateClient(string clientID, string secretKey)
        {
            var passed = _manager.Authenticate(clientID, secretKey);
            if (passed)
            {
                _clientID = clientID;
            }
            return passed;
        }

        public string AddService(ServiceDescriptor descriptor)
        {
            if (Authenticated)
                return _manager.AddService(_clientID, descriptor);
            else
                return "";
        }

        public ServiceDescriptor GetService(string name)
        {
            if (Authenticated)
                return _manager.GetService(clientID: _clientID, name: name);
            else
                return new ServiceDescriptor();
        }

        public List<ServiceDescriptor> ListService()
        {
            if (Authenticated)
                return _manager.ListService(_clientID);
            else
                return new List<ServiceDescriptor>();
        }

        public void RemoveService(string name, string serviceToken)
        {
            if (Authenticated) _manager.RemoveService(_clientID, name, serviceToken);
        }

        public void ReportServiceHealthy(string name, string serviceToken)
        {
            if (Authenticated) _manager.ReportServiceHealthy(_clientID, name, serviceToken);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _manager.ServiceDeadEvent -= ServiceDeadEvent;
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
