/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
using GSerialize;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XPRPC.Client;
using XPRPC.Server;

namespace XPRPC
{
    public class DataChannel : IDisposable, IDataSender
    {
        private readonly ExclusiveLock _readingLock = new ExclusiveLock();
        private readonly ExclusiveLock _writtingLock = new ExclusiveLock();
        private readonly Dictionary<Int32, ResponseTicket> _pendingRequests = new Dictionary<Int32, ResponseTicket>();
        private readonly Pool<ResponseTicket> _ticketPool = new Pool<ResponseTicket>(limit: 10);
        private readonly ServiceCache _serviceCache = new ServiceCache();
        private readonly ProxyCache _proxyCache = new ProxyCache();
        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
        private bool _running = false;

        private readonly List<InteractiveBlock> s_ProtoTypes = new List<InteractiveBlock>
        {
            new BlockObjectCall(),
            new BlockObjectReply(),
            new BlockObjectEvent(),
        };
        

        public Stream DataStream { get; set; }

        public DataChannel()
        {
        }

        public async Task InteractWithRemoteAsync()
        {
            try
            {
                _running = true;
                var packer = new Packer(DataStream);
                while (!_cancellation.IsCancellationRequested)
                {
                    using var block = await ReadBlockStreamAsync(packer);
                    HandleBlockAsync(block);
                }
            }
            finally
            {
                ClearPendingRequest("RPC error");
                lock(_cancellation)
                {
                    _running = false;
                    Monitor.PulseAll(_cancellation);
                }
            }
        }

        private async void HandleBlockAsync(MemoryStream dataBlock)
        {
            var packer = new Packer(dataBlock);
            var key = (BlockKey)packer.ReadByte();
            foreach (var p in s_ProtoTypes)
            {
                if (p.Key == key)
                {
                    var block = p.ReadFromStream(dataBlock);
                    await block.CallChannel(this);
                    break;
                }                
            }
        }

        private async Task<MemoryStream> ReadBlockStreamAsync(Packer packer)
        {
            using var locker = _readingLock.Lock(); //read a whole block exclusively
            var len = await packer.ReadInt32Async(_cancellation.Token);
            if (len > 0)
            {
                var bytes = new byte[len];
                await packer.ReadNBytesAsync(bytes, len, _cancellation.Token);
                return new MemoryStream(bytes, 0, len);
            }
            return new MemoryStream();
        }

        private async Task SendBlockAsync(InteractiveBlock block)
        {
            using var memStream = new MemoryStream();
            var packer = new Packer(memStream);
            packer.WriteInt32(0);
            packer.WriteByte((byte)block.Key);
            block.WriteToStream(memStream);
            memStream.Seek(0, SeekOrigin.Begin);
            packer.WriteInt32((Int32)memStream.Length - 4);
            memStream.Seek(0, SeekOrigin.Begin);

            using var locker = _writtingLock.Lock(); //wite a whole block exclusively
            await memStream.CopyToAsync(DataStream);
        }

        #region IDataSender       

        public async void SendEvent<TEventArgs>(Int16 objectID, Int16 methodID, TEventArgs args)
        where TEventArgs : EventArgs
        {
            using var stream = new MemoryStream();
            var serializer = new Serializer(stream);
            serializer.Serialize(args);
            var block = new BlockObjectEvent
            {
                ObjectID = objectID,
                EventID = methodID,
                DataStream = stream
            };

            await SendBlockAsync(block);
        }

        public MemoryStream SendCallRequest(Int16 objectID, Int16 methodID, byte[] requestData)
        {
            ResponseTicket ticket;
            lock (_pendingRequests)
            {
                ticket = _ticketPool.Get();
                ticket.Reset(forAsync: false);
                _pendingRequests[ticket.RequestSerial] = ticket;
            }

            try
            {
                InteractiveBlock block = new BlockObjectCall
                {
                    RequestID = ticket.RequestSerial,
                    ObjectID = objectID,
                    MethodID = methodID,
                    DataStream = new MemoryStream(requestData)
                };
                _ = SendBlockAsync(block);
                return ticket.GetResponseData();
            }
            finally
            {
                OnTicketDone(ticket);
            }
        }

        public async Task<MemoryStream> SendCallRequestAsync(Int16 objectID, Int16 methodID, byte[] requestData)
        {
            ResponseTicket ticket;
            lock (_pendingRequests)
            {
                ticket = _ticketPool.Get();
                ticket.Reset(forAsync: true);
                _pendingRequests[ticket.RequestSerial] = ticket;
            }

            try
            {
                InteractiveBlock block = new BlockObjectCall
                {
                    RequestID = ticket.RequestSerial,
                    ObjectID = objectID,
                    MethodID = methodID,
                    DataStream = new MemoryStream(requestData)
                };
                _ = SendBlockAsync(block);
                var result = await ticket.ResponseSource.Task;
                OnTicketDone(ticket);
                return result;
            }
            catch
            {
                OnTicketDone(ticket);
                throw;
            }
        }

        private void OnTicketDone(ResponseTicket ticket)
        {
            lock (_pendingRequests)
            {
                if (_pendingRequests.ContainsKey(ticket.RequestSerial))
                {
                    _pendingRequests.Remove(ticket.RequestSerial);
                }
                _ticketPool.Recycle(ticket);
            }
        }

        public Int16 CacheService<TService>(TService service)
        {
            return _serviceCache.CacheServlet(service, this);
        }

        public TService CacheProxy<TService>(Int16 remoteObjectID)
        {
            return _proxyCache.CacheProxy<TService>(remoteObjectID, this);
        }

        #endregion IDataSender

        protected virtual void OnChannelCall(BlockObjectCall block, MemoryStream resultStream)
        {
        }

        protected virtual bool CanCall(BlockObjectCall block)
        {
            return true;
        }

        internal async Task OnObjectCall(BlockObjectCall request)
        {                        
            var reply = new BlockObjectReply{ RequestID = request.RequestID };
            try
            {
                if (CanCall(request))
                {
                    var resultStream = new MemoryStream();
                    if (request.ObjectID < 0) 
                    {
                        OnChannelCall(request, resultStream);
                    }
                    else
                    {
                        _serviceCache.CallObject(request, resultStream);
                    }
                    reply.Success = true;
                    reply.DataStream = resultStream;
                }
                else
                {
                    var ex = new UnauthorizedAccessException("Access token need be verified");
                    reply = BlockObjectReply.BuildFromException(ex, request.RequestID);
                }
            }
            catch(Exception ex)
            {
                reply = BlockObjectReply.BuildFromException(ex, request.RequestID);
            }
            await SendBlockAsync(reply);
        }

        internal Task OnObjectReply(BlockObjectReply block)
        {
            if (block.Success)
            {
                OnResult(block);
            }
            else
            {
                var exceptionMessage = new Packer(block.DataStream).ReadString();
                OnRemoteException(exceptionMessage, block.RequestID);
            }
            return Task.CompletedTask;
        }

        private void OnRemoteException(string exceptionMessage, int requestID)
        {
            lock(_pendingRequests)
            {
                if (_pendingRequests.TryGetValue(requestID, out ResponseTicket ticket))
                {
                    ticket.SetException(exceptionMessage);
                    _pendingRequests.Remove(requestID);
                }
            }
        }

        private void OnResult(BlockObjectReply block)
        {
            lock(_pendingRequests)
            {
                if (_pendingRequests.TryGetValue(block.RequestID, out ResponseTicket ticket))
                {
                    ticket.SetResponseData(block.DataStream);
                    _pendingRequests.Remove(block.RequestID);
                }
            }
        }

        internal Task OnObjectEvent(BlockObjectEvent block)
        {
            if (block.ObjectID < 0) return OnChannelEvent(block);
            _proxyCache.HandleEvent(block);
            return Task.CompletedTask;
        }

        protected virtual Task OnChannelEvent(BlockObjectEvent block)
        {
            return Task.CompletedTask;
        }

        private void ClearPendingRequest(string reason)
        {
            lock (_pendingRequests)
            {
                foreach (var ticket in _pendingRequests.Values)
                {
                    ticket.SetException(reason);
                }
                _pendingRequests.Clear();
            }
        }

        private void StopInteraction()
        {
            _cancellation.Cancel();
            lock (_cancellation)
            {
                while (_running) Monitor.Wait(_cancellation);
            }
        }

        #region IDisposable

        public bool Disposed { get; private set; }
        protected virtual void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    _proxyCache.Dispose();
                    _ticketPool.Dispose();                   
                    StopInteraction();
                }
                Disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
