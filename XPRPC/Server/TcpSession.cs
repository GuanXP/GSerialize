/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GSerialize;

namespace XPRPC.Server
{
    public class TcpSession<TService> : IDisposable
    {        
        private readonly ServerDataChannel<TService> _dataChannel;

        public TcpSession(TService service, Stream clientStream, String accessToken)
        {
            _dataChannel = new ServerDataChannel<TService>(service, accessToken);
            SetClientStream(clientStream);
        }

        public void SetClientStream(Stream clientStream)
        {
            _dataChannel.DataStream = clientStream;
        }

        public  Task InteractWithClientAsync()
        {
            return _dataChannel.InteractWithRemoteAsync();
        }

        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _dataChannel?.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    public class ServerDataChannel<TService> : DataChannel
    {
        public TService Service{get; private set;}
        public bool AccessGranted{ get; private set;}

        private readonly string _accessToken;
        private delegate void ChannelCall(Stream dataStream, MemoryStream resultStream);
        private readonly Dictionary<Int16, ChannelCall> _channelCalls = new Dictionary<Int16, ChannelCall>();

        private static readonly string s_InterfaceName = typeof(TService).VersionName();

        public ServerDataChannel(TService service, string accessToken)
        {   
            Service = service;
            _accessToken = accessToken;
            CacheService<TService>(service);
            _channelCalls[901] = VerifyInterfaceName;
            _channelCalls[902] = VerifyAccessToken;
            _channelCalls[903] = ClientPing;
        }

        protected override void OnChannelCall(BlockCall block, MemoryStream resultStream)
        {
            if (_channelCalls.TryGetValue(block.MethodID, out ChannelCall method))
            {
                method(block.DataStream, resultStream);
            }
            else
            {
                throw new InvalidOperationException($"未能找到合适方法id = {block.MethodID}");
            }
        }

        protected override bool CanCall(BlockCall block)
        {
            if (block.ObjectID < 0 && _channelCalls.ContainsKey(block.MethodID))
            {
                return true;
            }
            return AccessGranted;
        }

        private void VerifyInterfaceName(Stream dataStream, MemoryStream resultStream)
        {
            var packer = new Packer(dataStream);
            var name = packer.ReadString();
            var matched = s_InterfaceName == name;

            var resultPacker = new Packer(resultStream);
            resultPacker.WriteBool(matched);
            if (!matched)
            {
                resultPacker.WriteString(typeof(TService).FullName);
            }
        }

        private void VerifyAccessToken(Stream dataStream, MemoryStream resultStream)
        {
            var packer = new Packer(dataStream);
            var accessToken = packer.ReadString();
            AccessGranted = accessToken == _accessToken;

            var resultPacker = new Packer(resultStream);
            resultPacker.WriteBool(AccessGranted);
        }

        private void ClientPing(Stream dataStream, MemoryStream resultStream)
        {
            System.Diagnostics.Debug.WriteLine("ping from client");
        }
    }
}