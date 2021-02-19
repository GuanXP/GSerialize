/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
using System;
using System.Collections.Generic;
using System.Linq;

namespace XPRPC.Server
{
    class ServiceRecord
    {
        internal ServiceDescriptor Descriptor;
        internal string AccessToken;
        internal DateTime LastHealthyChecked = DateTime.Now;
        internal string Publisher;
    }

    public class ServiceManager
    {
        Dictionary<string, ServiceRecord> _services = new Dictionary<string, ServiceRecord>();

        AccessConfig _accessConfig = new AccessConfig();

        object _lock = new object();

        public void Config(AccessConfig config)
        {
            _accessConfig = config;
        }

        public string AddService(string clientID, ServiceDescriptor descriptor)
        {
            lock (_lock)
            {
                if (!_accessConfig.ClientIsServiceProvider(clientID))
                {
                    return "";
                }
                if (_services.ContainsKey(descriptor.Name)) return "";

                var copy = new Dictionary<string, ServiceRecord>(_services);
                var accessToken = TokenGenerator.RandomToken(32);
                copy[descriptor.Name] = new ServiceRecord
                {
                    Descriptor = descriptor,
                    AccessToken = accessToken,
                    Publisher = clientID
                };
                _services = copy;
                return accessToken;
            }
        }

        public void RemoveService(string clientID, string name, string accessToken)
        {
            lock (_lock)
            {
                if (!_accessConfig.ClientIsServiceProvider(clientID))
                {
                    return;
                }
                if (_services.TryGetValue(name, out ServiceRecord record))
                {
                    if (accessToken == record.AccessToken && record.Publisher == clientID)
                    {
                        var copy = new Dictionary<string, ServiceRecord>(_services);
                        copy.Remove(record.Descriptor.Name);
                        _services = copy;
                    }
                }
            }
        }

        public void ReportServiceHealthy(string clientID, string name, string accessToken)
        {
            lock (_lock)
            {
                if (!_accessConfig.ClientIsServiceProvider(clientID)) return;
                if (_services.TryGetValue(name, out ServiceRecord record))
                {
                    if (accessToken == record.AccessToken)
                    {
                        record.LastHealthyChecked = DateTime.Now;
                    }
                }
            }
        }

        public void CheckHealthy()
        {
            var expiredServices = new List<string>();
            lock (_lock)
            {                
                var deadline = DateTime.Now - new TimeSpan(hours: 0, minutes: 2, seconds: 0);
                foreach(var item in _services)
                {
                    if (item.Value.LastHealthyChecked < deadline)
                    {
                        expiredServices.Add(item.Key);
                    }
                }
                foreach(var name in expiredServices)
                {
                    _services.Remove(name);
                }
            }

            foreach(var name in expiredServices)
            {
                _services.Remove(name);
                NotifyServiceDead(name);
            }
        }

        private void NotifyServiceDead(string serviceName)
        {
            var handler = ServiceDeadEvent;
            handler?.Invoke(this, new ServiceDeadEventArgs{ServiceName = serviceName});
        }

        public bool Authenticate(string clientID, string secretKey)
        {
            lock (_lock)
            {
                return _accessConfig.ClientIsValid(clientID, secretKey);
            }
        }

        public ServiceDescriptor GetService(string clientID, string name)
        {
            lock (_lock)
            {
                _services.TryGetValue(name, out ServiceRecord record);
                if (record != null)
                {
                    if (record.Publisher == clientID)
                        return record.Descriptor;
                    if (_accessConfig.ClientCanAccessService(clientID, name))
                        return record.Descriptor;
                }
                return new ServiceDescriptor();
            }
        }

        public List<ServiceDescriptor> ListService(string clientID)
        {
            lock (_lock)
            {
                return new List<ServiceDescriptor>(
                    from record in _services.Values
                    where _accessConfig.ClientCanAccessService(clientID, record.Descriptor.Name)
                    select record.Descriptor
                    );
            }
        }

        public event EventHandler<ServiceDeadEventArgs> ServiceDeadEvent;
    }
}
