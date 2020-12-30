/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace XPRPC.Server
{
    public abstract class ManagerRunner
    {        
        private readonly Timer _timerCheckHealthy;
        protected ManagerRunner()
        {
            _timerCheckHealthy = new Timer(
                callback: OnTimerCheckHealthy, 
                state: null, 
                dueTime: Timeout.Infinite, 
                period: 300_000);
        }
        public void Config(AccessConfig config)
        {
            Service.Config(config);
        }
        public abstract void Start(ServiceDescriptor descriptor, X509Certificate sslCertificate);
        public virtual void Stop()
        {
            _timerCheckHealthy.Change(dueTime: Timeout.InfiniteTimeSpan, period: Timeout.InfiniteTimeSpan);
        }

        protected void StartCheckHealthy()
        {            
            _timerCheckHealthy.Change(dueTime: 0, period: 300_000);            
        }

        private void OnTimerCheckHealthy(Object state)
        {
            Service.CheckHealthy();
        }

        public IServiceManager CreateSession()
        {
            return new ServiceManagerSession(Service);
        }

        protected ServiceManager Service { get; } = new ServiceManager();
    }
}
