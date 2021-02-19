/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace XPRPC
{
    public class ExclusiveLock
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private int _reentrances = 0;
        internal readonly SemaphoreSlim _retry = new SemaphoreSlim(0, 1);
        private const long UnlockedId = 0x00; // "owning" task id when unlocked
        internal long _owningId = UnlockedId;
        internal int _owningThreadId = (int) UnlockedId;
        private static long AsyncStackCounter = 0;
        private static readonly AsyncLocal<long> _asyncId = new AsyncLocal<long>();
        private static long AsyncId => _asyncId.Value;

        private static int ThreadId => Thread.CurrentThread.ManagedThreadId;

        public ExclusiveLock()
        {
        }

        struct InnerLock : IDisposable
        {
            private readonly ExclusiveLock _parent;
            private readonly long _oldId;
            private readonly int _oldThreadId;
            private bool _disposed;

            internal InnerLock(ExclusiveLock parent, long oldId, int oldThreadId)
            {
                _parent = parent;
                _oldId = oldId;
                _oldThreadId = oldThreadId;
                _disposed = false;
            }

            internal async Task<IDisposable> ObtainLockAsync(CancellationToken ct = default)
            {
                while (!await TryEnterAsync())
                {
                    // We need to wait for someone to leave the lock before trying again.
                    await _parent._retry.WaitAsync(ct);
                }
                // Reset the owning thread id after all await calls have finished, otherwise we
                // could be resumed on a different thread and set an incorrect value.
                _parent._owningThreadId = ThreadId;
                // In case of !synchronous and success, TryEnter() does not release the _semaphore lock
                _parent._semaphore.Release();
                return this;
            }

            internal IDisposable ObtainLock()
            {
                while (!TryEnter())
                {
                    // We need to wait for someone to leave the lock before trying again.
                    _parent._retry.Wait();
                }
                return this;
            }

            private async Task<bool> TryEnterAsync()
            {
                await _parent._semaphore.WaitAsync();
                return InnerTryEnter();
            }

            private bool TryEnter()
            {
                _parent._semaphore.Wait();
                return InnerTryEnter(synchronous: true);
            }

            private bool InnerTryEnter(bool synchronous = false)
            {
                bool result = false;
                try
                {
                    if (synchronous)
                    {
                        if (_parent._owningThreadId == UnlockedId)
                        {
                            _parent._owningThreadId = ThreadId;
                        }
                        else if (_parent._owningThreadId != ThreadId)
                        {
                            return false;
                        }
                        _parent._owningId = ExclusiveLock.AsyncId;
                    }
                    else
                    {
                        if (_parent._owningId == UnlockedId)
                        {
                            // Obtain a new async stack ID
                            //_asyncId.Value = Interlocked.Increment(ref AsyncLock.AsyncStackCounter);
                            _parent._owningId = ExclusiveLock.AsyncId;
                        }
                        else if (_parent._owningId != _oldId)
                        {
                            // Another thread currently owns the lock
                            return false;
                        }
                        else
                        {
                            // Nested re-entrance
                            _parent._owningId = AsyncId;
                        }
                    }

                    // We can go in
                    Interlocked.Increment(ref _parent._reentrances);
                    result = true;
                    return result;
                }
                finally
                {
                    // We can't release this in case the lock was obtained because we still need to
                    // set the owning thread id, but we may have been called asynchronously in which
                    // case we could be currently running on a different thread than the one the
                    // locking will ultimately conclude on.
                    if (!result || synchronous)
                    {
                        _parent._semaphore.Release();
                    }
                }
            }

            public void Dispose()
            {
                Debug.Assert(!_disposed);
                if (_disposed) return;
                _disposed = true;

                var self = this;
                var oldId = this._oldId;
                var oldThreadId = this._oldThreadId;
                Task.Run(async () =>
                {
                    await self._parent._semaphore.WaitAsync();
                    try
                    {
                        Interlocked.Decrement(ref self._parent._reentrances);
                        self._parent._owningId = oldId;
                        self._parent._owningThreadId = oldThreadId;
                        if (self._parent._reentrances == 0)
                        {
                            // The owning thread is always the same so long as we
                            // are in a nested stack call. We reset the owning id
                            // only when the lock is fully unlocked.
                            self._parent._owningId = UnlockedId;
                            self._parent._owningThreadId = (int)UnlockedId;
                            if (self._parent._retry.CurrentCount == 0)
                            {
                                self._parent._retry.Release();
                            }
                        }
                    }
                    finally
                    {
                        self._parent._semaphore.Release();
                    }
                });
            }
        }

        // Make sure InnerLock.LockAsync() does not use await, because an async function triggers a snapshot of
        // the AsyncLocal value.
        public Task<IDisposable> LockAsync(CancellationToken ct = default)
        {
            var locker = new InnerLock(this, _asyncId.Value, ThreadId);
            _asyncId.Value = Interlocked.Increment(ref ExclusiveLock.AsyncStackCounter);
            return locker.ObtainLockAsync(ct);
        }

        public IDisposable Lock()
        {
            var locker = new InnerLock(this, _asyncId.Value, ThreadId);
            // Increment the async stack counter to prevent a child task from getting
            // the lock at the same time as a child thread.
            _asyncId.Value = Interlocked.Increment(ref ExclusiveLock.AsyncStackCounter);
            return locker.ObtainLock();
        }
    }
}