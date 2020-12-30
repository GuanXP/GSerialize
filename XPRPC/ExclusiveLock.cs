/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
using System;
using System.Threading;

namespace XPRPC
{
    public class ExclusiveLock
    {
        private readonly Object _lock = new object();
        private bool _locked = false;

        public void Acquire()
        {
            lock(_lock)
            {
                while(_locked)
                {
                    Monitor.Wait(_lock);
                }
                _locked = true;
            }
        }

        public void Release()
        {
            lock(_lock)
            {
                _locked = false;
                Monitor.Pulse(_lock);
            }
        }

        public Releaser Lock()
        {
            return new Releaser(this);
        }

        public struct Releaser : IDisposable
        {
            readonly ExclusiveLock _lock;

            internal Releaser(ExclusiveLock exc)
            {
                _lock = exc;
                _lock.Acquire();
            }

            public void Dispose()
            {
                _lock.Release();
            }
        }
    }
}