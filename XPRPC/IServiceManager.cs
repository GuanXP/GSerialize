/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
using System;
using System.Collections.Generic;

namespace XPRPC
{
    public interface IServiceManager : IDisposable
    {
        /// <summary>
        /// Authenticate a client
        /// </summary>
        /// <param name="clientID">the client ID</param>
        /// <param name="secretKey">the secret key for the client</param>
        /// <returns></returns>
        bool AuthenticateClient(string clientID, string secretKey);

        /// <summary>
        /// Add a new service
        /// </summary>
        /// <param name="descriptor">service descriptor</param>
        /// <returns>
        /// A service token to operate the service later if succeeded;
        /// An empty string if failed
        /// </returns>
        string AddService(ServiceDescriptor descriptor);

        /// <summary>
        /// Remove an existing service
        /// </summary>
        /// <param name="name">servie name</param>
        /// <param name="serviceToken">The service token returned from @AddService</param>
        void RemoveService(string name, string serviceToken);

        /// <summary>
        /// Report a service is alive. If an service can't report itself alive in a period, 
        /// service manager will judge it dead and remove it from the active services so that
        ///  client can't access it any more.
        /// </summary>
        /// <param name="name">service name</param>
        /// <param name="serviceToken">The service token returned from @AddService</param>
        void ReportServiceHealthy(string name, string serviceToken);

        /// <summary>
        /// Get a service by name
        /// </summary>
        /// <param name="name">service name</param>
        /// <returns>
        /// The service descriptor if the service is running and the client can access it.
        /// otherwise a default service descriptor without any information returned.
        /// </returns>
        ServiceDescriptor GetService(string name);

        /// <summary>
        /// Get all services that a client can access.
        /// </summary>
        /// <returns>list of service descriptors</returns>
        List<ServiceDescriptor> ListService();

        event EventHandler<ServiceDeadEventArgs> ServiceDeadEvent;
    }

    public static class ServiceManagerExtension
    {
        public static bool ServiceExists(this IServiceManager serviceManager, string servieName)
        {
            return !string.IsNullOrEmpty(serviceManager.GetService(servieName).Name);
        }
    }

    [Serializable]
    public sealed class ServiceDeadEventArgs: EventArgs
    {
        public string ServiceName {get; set;}
    }
}
