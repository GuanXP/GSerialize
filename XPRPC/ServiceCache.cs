/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
using System;
using System.Collections.Generic;
using System.IO;

namespace XPRPC
{
    class ServiceCache
    {
        private List<ServiceItem> _items = new List<ServiceItem>();
        private Dictionary<Int16, ServiceItem> _soretedItems = new Dictionary<Int16, ServiceItem>();

        public Int16 CacheServlet<TService>(TService service, IDataSender sender)
        {
            lock(_items)
            {
                var item = _items.Find(x=> Object.ReferenceEquals(x.Service, service));
                if (item == null)
                {
                    item = ServiceItem.Build<TService>(service, sender);
                    if (_items.Count == 0) item.ObjectID = 0; //The first service object takes the fixed ID 0
                    _items.Add(item);
                    _soretedItems.Add(item.ObjectID, item);
                }
                return item.ObjectID;
            }
        }

        private ServiceItem FindByID(Int16 objectID)
        {
            lock(_items)
            {
                _soretedItems.TryGetValue(objectID, out ServiceItem item);
                return item;
            }
        }

        internal void CallObject(BlockCall block, MemoryStream resultStream)
        {
            var service = FindByID(block.ObjectID);
            if (service != null)
            {
                service.Dispatcher.DispatchMethodCall(block.MethodID, block.DataStream, resultStream);
            }
            else
            {
                throw new NullReferenceException($"Can't find service object with id={block.ObjectID}");
            }
        }        
    }
}