/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
using System;
using System.IO;
using System.Threading.Tasks;

namespace XPRPC
{
    public interface IDataSender
    {        
        void SendEvent<TEventArgs>(Int16 objectID, Int16 eventID, TEventArgs args) where TEventArgs: EventArgs;
        public MemoryStream SendCallRequest(Int16 objectID, Int16 methodID, byte[] serializedParam);
        public Task<MemoryStream> SendCallRequestAsync(Int16 objectID, Int16 methodID, byte[] serializedParam);
        Int16 CacheService<TService>(TService service);
        TService CacheProxy<TService>(Int16 remoteObjectID);
    }
}