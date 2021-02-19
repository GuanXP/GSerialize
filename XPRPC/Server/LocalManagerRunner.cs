/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace XPRPC.Server
{
    public class LocalManagerRunner : ManagerRunner, IDisposable
    {
        public static LocalManagerRunner Instance { get; } = new LocalManagerRunner();

        private LocalManagerRunner()
        {            
        }

        public override void Start(ServiceDescriptor descriptor, X509Certificate sslCertificate)
        {
            if (_thread == null)
            {
                _eventQuit.Reset();
                _thread = new Thread(TimedCheckHealthy);
                _thread.Start();
                StartCheckHealthy();
            }
        }

        public override void Stop()
        {
            base.Stop();
            if (_thread != null)
            {
                _eventQuit.Set();
                _thread.Join();
                _thread = null;
            }
        }

        private void TimedCheckHealthy()
        {
            while(true)
            {
                if (_eventQuit.Wait(millisecondsTimeout: 15000)) break;
                StartCheckHealthy();
            }
        }

        internal void AddService(string name, Object service)
        {
            lock(_lockServiceRegistrant)
            {
                _registeredServices[name] = service;
            }
        }

        internal Object FindService(string name)
        {
            lock (_lockServiceRegistrant)
            {
                _registeredServices.TryGetValue(name, out Object service);
                return service;
            }
        }

        internal void RemoveService(string name)
        {
            lock (_lockServiceRegistrant)
            {
                _registeredServices.Remove(name);
            }
        }

        ManualResetEventSlim _eventQuit = new ManualResetEventSlim(false);
        Thread _thread;
        Dictionary<string, Object> _registeredServices = new Dictionary<string, Object>();
        object _lockServiceRegistrant = new object();

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Stop();
                    _eventQuit.Dispose();
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
