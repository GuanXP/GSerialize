/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
using System;

namespace XPRPC.Server
{
    /// <summary>
    /// Publisher for local services
    /// </summary>
    public class LocalServiceRunner<TService> : ServiceRunner<TService>
        where TService : IDisposable
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="service">the concrete service object</param>
        /// <param name="descriptor">service descriptor</param>        
        /// <param name="holdService">if ServiceRunner need hold the service object and dispose it when disposing</param>
        public LocalServiceRunner(TService service, ServiceDescriptor descriptor, bool holdService)
        : base(service, descriptor, holdService)
        {
            LocalManagerRunner.Instance.AddService(descriptor.Name, service);
        }

        protected override void Dispose(bool disposing)
        {
            LocalManagerRunner.Instance.RemoveService(Descriptor.Name);
            base.Dispose(disposing);
        }

        protected override IServiceManager ResolveServiceManager(ServiceDescriptor descriptor)
        {
            return LocalManagerRunner.Instance.CreateSession();
        }
    }
}
