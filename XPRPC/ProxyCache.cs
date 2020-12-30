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
    class ProxyCache
    {
        private Dictionary<Int16, ProxyItem> _items = new Dictionary<Int16, ProxyItem>();

        public TService CacheProxy<TService>(Int16 remoteObjectID, IDataSender sender)
        {
            lock(_items)
            {
                if (!_items.TryGetValue(remoteObjectID, out ProxyItem item))
                {
                    item = ProxyItem.Build<TService>(remoteObjectID, sender);
                    _items.Add(item.RemoteObjectID, item);
                }
                return (TService)item.Proxy;
            }            
        }

        private ProxyItem FindByID(Int16 remoteObjectID)
        {
            lock(_items)
            {
                _items.TryGetValue(remoteObjectID, out ProxyItem item);
                return item;
            }
        }

        public void HandleEvent(BlockObjectEvent block)
        {
            FindByID(block.ObjectID)?.Proxy.FireEvent(block.EventID, block.DataStream);
        }
    }
}