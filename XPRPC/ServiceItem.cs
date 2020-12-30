/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
using System;

namespace XPRPC
{
    public class ServiceItem
    {
        public Object Service{get; private set;}
        public Type InterfaceType{get; private set;}
        public IDispatcher Dispatcher{get; private set;}

        private static Int16 s_nextID = 100;
        public Int16 ObjectID{ get; set; } = ++s_nextID;
        private IDataSender _sender;

        internal static ServiceItem Build<TService>(Object service, IDataSender sender)
        {
            System.Diagnostics.Debug.Assert(typeof(TService).IsInterface);
            System.Diagnostics.Debug.Assert(typeof(TService).IsAssignableFrom(service.GetType()));
            
            return new ServiceItem(typeof(TService), service, sender);
        }

        private ServiceItem(Type interfaceType, Object service, IDataSender sender)
        {
            Service = service;
            InterfaceType = interfaceType;
            _sender = sender;
            Dispatcher = DispatcherGenerator.Instance.GetDispatcher(this);
        }

        public void SendEvent<TEventArgs>(Int16 eventID, TEventArgs args)
        where TEventArgs: EventArgs
        {
            _sender.SendEvent(ObjectID, eventID, args);
        }

        public Int16 CacheService<TService>(TService service)
        {
            return _sender.CacheService(service);
        }
        public TService CacheProxy<TService>(Int16 remoteObjectID)
        {
            return _sender.CacheProxy<TService>(remoteObjectID);
        }
    }
}