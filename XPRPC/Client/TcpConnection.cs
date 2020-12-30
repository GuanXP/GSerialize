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
    class ConnectionCountEventArgs: EventArgs
    {
        public int Count {get; set;}
    }

    public class TcpConnection<TService>: IDisposable
    where TService : IDisposable
    {
        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
        private readonly ManualResetEventSlim _eventDone = new ManualResetEventSlim(false);
        private ClientDataChannel<TService> _dataChannel;
        private ServiceDescriptor ServiceDescriptor;        
        
        private bool disposedValue;
        private object _lock = new object();
        private readonly string _sessionKey = TokenGenerator.RandomToken(16);

        private int _connectionCount = 0;
        internal event EventHandler<ConnectionCountEventArgs> ConnectionCountEvent;
 
        public TcpConnection(ServiceDescriptor descriptor)
        {
            ServiceDescriptor = descriptor;
            _dataChannel = new ClientDataChannel<TService>(descriptor.AccessToken, _cancellation.Token);
        }

        public TService GetService()
        {
            return _dataChannel.CacheProxy<TService>(remoteObjectID: 0);
        }

        public void Connect()
        {
            if (_dataChannel.DataStream == null)
            {
                _eventDone.Reset();
                var client = new TcpClient();
                client.Connect(ServiceDescriptor.ServiceHost, ServiceDescriptor.ServicePort);
                _dataChannel.DataStream = MakeStream(client);
                SendSessionKey(_dataChannel.DataStream);
                Task.Run(InteractWithServer);
                
                _dataChannel.OnConnected();

                ++_connectionCount;
                ConnectionCountEvent?.Invoke(GetService(), new ConnectionCountEventArgs { Count = _connectionCount });
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
                #pragma warning disable CA5359
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

        private void Close()
        {
            _cancellation.Cancel();
            _eventDone.Wait();
            _dataChannel.DataStream?.Dispose();
            _dataChannel.DataStream = null;
        }

        private async void InteractWithServer()
        {
            try
            {
                await _dataChannel.InteractWithRemoteAsync();
            }
            catch(IOException)
            { 
                if (!_cancellation.IsCancellationRequested && TryReconnect())
                {
                    return;
                }
            }
            catch
            {
            }
            _eventDone.Set();
        }

        private bool TryReconnect()
        {
            _eventDone.Set();
            Close();
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

        public bool Disposed => disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Close();
                    _cancellation.Dispose();
                    _eventDone.Dispose();
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

    class ClientDataChannel<TService> : DataChannel
    where TService : IDisposable
    {
        private static readonly string s_InterfaceName = typeof(TService).VersionName();
        private readonly Timer _timerPing;
        private readonly string _serviceAccessToken;
        
        public ClientDataChannel(string serviceAccessToken, CancellationToken cancellationToken) : base(cancellationToken)
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

        protected bool Connected => DataStream != null;

        private void OnTimerPing(Object state)
        {
            try
            {
                if (Connected)
                {
                    Ping();
                }
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
                throw new Exception($"expect {s_InterfaceName}，but server site returns {remoteInterfaceName}");
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
                throw new AccessViolationException("Invalid access token");
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