/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
using System;
using XPRPC.Server;

namespace XPRPC.Client
{
    public sealed class LocalServiceResolver : ServiceResolver
    {
        public LocalServiceResolver(ServiceDescriptor serviceManagerDesc, string clientID, string secretKey) 
            : base(serviceManagerDesc, clientID, secretKey)
        {
        }

        protected override TService ResolveService<TService>(ServiceDescriptor descriptor)
        {
            if (string.IsNullOrEmpty(descriptor.Name))
            {
                throw new NotSupportedException($"No service found with type {typeof(TService).Name}");
            }

            return (TService)LocalManagerRunner.Instance.FindService(descriptor.Name);
        }

        protected override IServiceManager ResolveServiceManager(ServiceDescriptor descriptor)
        {
            return LocalManagerRunner.Instance.CreateSession();
        }

        protected override bool ServiceIsActive(Object service)
        {
            return true;
        }
    }
}
