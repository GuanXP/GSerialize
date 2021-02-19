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
    class ProxyCache : IDisposable
    {
        private Dictionary<Int16, ProxyItem> _items = new Dictionary<Int16, ProxyItem>();
        private LinkedList<ProxyItem> _orderedItems = new LinkedList<ProxyItem>();
        private bool disposedValue;

        public TService CacheProxy<TService>(Int16 remoteObjectID, IDataSender sender)
        {
            lock(_items)
            {
                if (!_items.TryGetValue(remoteObjectID, out ProxyItem item))
                {
                    item = ProxyItem.Build<TService>(remoteObjectID, sender);
                    _items.Add(item.RemoteObjectID, item);
                    _orderedItems.AddFirst(item); //�´����ķ���ǰ��
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

        public void HandleEvent(BlockEvent block)
        {
            FindByID(block.ObjectID)?.Proxy.FireEvent(block.EventID, block.DataStream);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    DisposeAll();
                }

                disposedValue = true;
            }
        }

        private void DisposeAll()
        {
            //Dispose˳���봴��˳���෴���Է��˴�֮��������
            foreach(var item in _orderedItems)
            {
                item.Dispose();
            }
            _orderedItems.Clear();
            _items.Clear();
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}