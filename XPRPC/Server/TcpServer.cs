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
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using XPRPC.Service.Logger;

namespace XPRPC.Server
{
    class SessionRecord<TService>
    {
        public TcpSession<TService> Session;
        public readonly DateTime DeactivateTime = DateTime.Now;
    }
    public class TcpServer<TService> : IDisposable
    {
        private TcpListener _listener;        
        private readonly ILogger _logger;
        private readonly ServiceDescriptor _descriptor;
        private readonly ManualResetEvent _eventDone = new ManualResetEvent(true);
        private readonly X509Certificate _sslCertificate;
        private readonly TService _service;
        private readonly Dictionary<string, SessionRecord<TService>> _inactiveSessions = new Dictionary<string, SessionRecord<TService>>();
        private bool _quit = false;

        public TcpServer(TService service, ServiceDescriptor descriptor, ILogger logger, X509Certificate sslCertificate)
        {
            _descriptor = descriptor;
            _logger = logger;
            _sslCertificate = sslCertificate;
            _service = service;
            if (string.IsNullOrEmpty(_descriptor.ServiceHost))
            {
                _descriptor.ServiceHost = Dns.GetHostName();
            }
            _listener = new TcpListener(IPAddress.Any, descriptor.ServicePort);
            _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _listener.Start(10);
            _descriptor.ServicePort = (_listener.LocalEndpoint as IPEndPoint).Port;            
            _logger.Info(tag: _descriptor.Name, message: "Service listening");

            Task.Run(Run);
        }

        private async void Run()
        {
            _eventDone.Reset();
            try
            {
                while(!_quit)
                {                   
                    var client = await _listener.AcceptTcpClientAsync();
                    _ = Task.Run(async () => await HandleClient(client));
                }
            }
            catch(Exception ex)
            {
                if (!_quit)
                {
                    _logger.Exception(tag:_descriptor.Name, ex);
                }
            }
            _eventDone.Set();
        }

        protected virtual TService SessionService => _service;

        private async Task HandleClient(TcpClient client)
        {
            using var stream = await MakeClientStream(client);
            TcpSession<TService> session = null;
            string sessionKey = "";
            try
            {
                sessionKey = new Packer(stream).ReadString();
                session = MakeSession(sessionKey, stream);
                await session.InteractWithClientAsync();
            }
            catch(Exception ex)
            {
                if (!(ex is IOException))
                {
                    _logger.Exception(tag:_descriptor.Name, ex);
                }
            }
            if (session != null) CacheInactiveSession(session, sessionKey);
        }

        private void CacheInactiveSession(TcpSession<TService> session, string sessionKey)
        {
            if (_quit)
            {
                session.Dispose();
                return;
            }
                
            lock (_inactiveSessions)
            {
                System.Diagnostics.Debug.Assert(!_inactiveSessions.ContainsKey(sessionKey));
                _inactiveSessions[sessionKey] = new SessionRecord<TService> { Session = session };
            }
        }

        private TcpSession<TService> MakeSession(string sessionKey, Stream stream)
        {
            lock (_inactiveSessions)
            {
                if (_inactiveSessions.ContainsKey(sessionKey)) //client reconnect, use the existing session
                {                    
                    var session = _inactiveSessions[sessionKey].Session;
                    _inactiveSessions.Remove(sessionKey);
                    session.SetClientStream(stream);
                    return session;
                }
                ClearExpiredInactiveSession();
            }
            return new TcpSession<TService>(
                    service: SessionService,
                    clientStream: stream,
                    accessToken: _descriptor.AccessToken
                );
        }

        private void ClearExpiredInactiveSession()
        {
            var removing = new List<string>();
            var now = DateTime.Now;
            foreach(var item in _inactiveSessions)
            {
                if ((now - item.Value.DeactivateTime).TotalSeconds > 60)
                {
                    removing.Add(item.Key);
                }
            }
            foreach(var k in removing)
            {
                _inactiveSessions[k].Session.Dispose();
                _inactiveSessions.Remove(k);
            }
        }

        private async Task<Stream> MakeClientStream(TcpClient client)
        {
            if (_sslCertificate != null)
            {
                var sslStream = new SslStream(client.GetStream(), leaveInnerStreamOpen: false);
                await sslStream.AuthenticateAsServerAsync(_sslCertificate);
                return sslStream;
            }
            else
            {
                return client.GetStream();
            }
        }

        private void Stop()
        {
            StopListener();
            DisposeInactiveSessions();
        }

        private void StopListener()
        {
            if (_listener != null)
            {
                _quit = true;
                _listener.Stop();
                _eventDone.WaitOne();
                _listener = null;
            }
        }

        private void DisposeInactiveSessions()
        {
            lock (_inactiveSessions)
            {
                foreach (var record in _inactiveSessions.Values)
                {
                    record.Session.Dispose();
                }
                _inactiveSessions.Clear();
            }
        }

        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Stop();
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
}