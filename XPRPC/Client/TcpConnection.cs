/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using GSerialize;

namespace XPRPC.Client
{
    public class TcpConnection<TService>: IDisposable
    {   
        private readonly ClientDataChannel<TService> _dataChannel;
        private readonly ServiceDescriptor ServiceDescriptor;
        private readonly string _sessionKey = TokenGenerator.RandomToken(16);
        private Stream _tcpStream;

        private int _connectionCount = 0;
 
        public TcpConnection(ServiceDescriptor descriptor)
        {
            ServiceDescriptor = descriptor;
            _dataChannel = new ClientDataChannel<TService>(descriptor.AccessToken);
            Connect();
        }

        public TService GetService()
        {
            return _dataChannel.CacheProxy<TService>(remoteObjectID: 0);
        }

        private void Connect()
        {
            _tcpStream?.Dispose();
            _tcpStream = null;

            var client = new TcpClient();
            client.Connect(ServiceDescriptor.ServiceHost, ServiceDescriptor.ServicePort);            
            _tcpStream = MakeStream(client);                
            SendSessionKey(_tcpStream);

            _dataChannel.DataStream = _tcpStream;
            Task.Run(InteractWithServer);
            ++_connectionCount;
            if (_connectionCount == 1)
            {
                _dataChannel.OnConnected();
            }
        }

        private void SendSessionKey(Stream stream)
        {
            var packer = new Packer(stream);
            packer.WriteString(_sessionKey);
        }

        private Stream MakeStream(TcpClient client)
        {
            if (ServiceDescriptor.UseSSL)
            {
                var sslStream = new SslStream(
                client.GetStream(), 
                leaveInnerStreamOpen: false,
                #pragma warning disable CA5359 //accept all certificates
                userCertificateValidationCallback: (sender, certificate, chain, sslPolicyErrors) => true);
                #pragma warning restore CA5359
                sslStream.AuthenticateAsClient("");
                return sslStream;
            }
            else
            {
                return client.GetStream();
            }
        }

        private async void InteractWithServer()
        {
            try
            {
                await _dataChannel.InteractWithRemoteAsync();
            }
            catch(IOException)
            { 
                if (!_dataChannel.Disposed && TryReconnect()) //网络异常，重连
                {
                    return;
                }
            }
            catch
            {
            }
        }

        private bool TryReconnect()
        {
            try
            {
                Connect(); 
                return true;
            } 
            catch 
            {
                Debug.WriteLine("Server unreachable");
            }
            return false;
        }

        public bool Disposed { get; private set; }
        protected virtual void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {                    
                    //这里有严格次序要求
                    _dataChannel.Dispose();
                    _tcpStream?.Dispose();
                    _tcpStream = null;
                }
                Disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    class ClientDataChannel<TService> : DataChannel
    {
        private static readonly string s_InterfaceName = typeof(TService).VersionName();
        private readonly Timer _timerPing;
        private readonly string _serviceAccessToken;
        
        public ClientDataChannel(string serviceAccessToken)
        {
            _timerPing = new Timer(callback: OnTimerPing, state: null, dueTime: Timeout.Infinite, period: 100_000);
            _serviceAccessToken = serviceAccessToken;
        }

        internal void OnConnected()
        {
            VerifyInterfaceName();
            VerifyAccessToken();
            _timerPing.Change(dueTime: 10_000, period: 100_000);
        }

        private void OnTimerPing(Object state)
        {
            try
            {
                Ping();
            }
            catch
            {

            }
        }

        private void VerifyInterfaceName()
        {
            using var stream = new MemoryStream();
            var packer = new Packer(stream);
            packer.WriteString(s_InterfaceName);
            var response = SendCallRequest(objectID: -1, methodID: 901, requestData: stream.ToArray());
            packer = new Packer(response);
            var serviceSupported = packer.ReadBool();
            if (!serviceSupported)
            {
                var remoteInterfaceName = packer.ReadString();
                throw new Exception($"期望{s_InterfaceName}，但服务器端为{remoteInterfaceName}");
            }
        }

        private void VerifyAccessToken()
        {
            using var stream = new MemoryStream();
            var packer = new Packer(stream);
            packer.WriteString(_serviceAccessToken);
            var response = SendCallRequest(objectID: -1, methodID: 902, requestData: stream.ToArray());
            packer = new Packer(response);
            var granted = packer.ReadBool();
            if (!granted)
            {
                throw new AccessViolationException("无效的安全码");
            }
            CacheProxy<TService>(remoteObjectID: 0);
        }

        public void Ping()
        {
            using var stream = new MemoryStream();
            SendCallRequest(objectID: -1, methodID: 903, requestData: stream.ToArray());
        }

        protected override void Dispose(bool disposing)
        {
            if (!Disposed && disposing)
            {
                _timerPing.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}