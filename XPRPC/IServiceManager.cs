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
    /// <summary>
    /// Manager to maintain services
    /// </summary>
    public interface IServiceManager : IDisposable
    {
        /// <summary>
        /// Authenticate a client is a predefined one
        /// </summary>
        /// <param name="clientID">the predfined client ID</param>
        /// <param name="secretKey">security key that only the client knows</param>
        /// <returns></returns>
        bool AuthenticateClient(string clientID, string secretKey);

        /// <summary>
        /// Register a service
        /// </summary>
        /// <param name="descriptor">the service descriptor</param>
        /// <returns>
        /// return a service token that can be used to operate the service later if succeed,
        /// or a null string if failure.
        /// </returns>
        string AddService(ServiceDescriptor descriptor);

        /// <summary>
        /// Unregister a service
        /// </summary>
        /// <param name="name">the service name</param>
        /// <param name="serviceToken">the service token returned from @AddService</param>
        void RemoveService(string name, string serviceToken);

        /// <summary>
        /// The service report itself healthy timely otherwise the ServiceManager
        /// will judge it dead an remove it from the active service list.
        /// </summary>
        /// <param name="name">servie name</param>
        /// <param name="serviceToken">the service token returned from @AddService</param>
        void ReportServiceHealthy(string name, string serviceToken);

        /// <summary>
        /// Try to query a service's descriptor
        /// </summary>
        /// <param name="name">service name</param>
        /// <returns>
        /// an available service descriptor if the service is alive and current client can access it,
        /// otherwise a empty descriptor
        /// </returns>
        ServiceDescriptor GetService(string name);

        /// <summary>
        /// list all service descriptors that can be accessed by current client
        /// </summary>
        /// <returns>service descriptors list</returns>
        List<ServiceDescriptor> ListService();

        /// <summary>
        /// event to notify a service death.
        /// </summary>
        event EventHandler<ServiceDeadEventArgs> ServiceDeadEvent;
    }

    public static class ServiceManagerExtension
    {
        /// <summary>
        /// check if a service exists
        /// </summary>
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
