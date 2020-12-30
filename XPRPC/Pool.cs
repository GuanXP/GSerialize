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
    class Pool<TObject> : IDisposable where TObject: IDisposable, new()
    {
        readonly Queue<TObject> _pool = new Queue<TObject>();
        readonly int _limit;
        public Pool(int limit)
        {
            _limit = limit;
        }

        public void Dispose()
        {
            foreach(var value in _pool) value.Dispose();
            _pool.Clear();
        }

        public TObject Get()
        {
            if (_pool.Count > 0)
            {
                return _pool.Dequeue();
            }
            return new TObject();
        }

        public void Recycle(TObject value)
        {
            if (_pool.Count < _limit)
            {
                _pool.Enqueue(value);
            }
            else
            {
                value.Dispose();
            }
        }
    }

}