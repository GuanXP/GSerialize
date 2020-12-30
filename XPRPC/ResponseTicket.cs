/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using XPRPC.Server;

namespace XPRPC
{

    class ResponseTicket: IDisposable
    {
        static Int32 NextRequestSerial = 100;
        static readonly Object SerialLock = new object();

        readonly MemoryStream _responseData = new MemoryStream();
        string _exceptionMessage;
        ManualResetEventSlim _eventDone = new ManualResetEventSlim(false);

        public Int32 RequestSerial{get; private set;}

        public TaskCompletionSource<MemoryStream> ResponseSource{get; private set;}
        public ResponseTicket()
        {
            UpdateSerial();
        }

        private void UpdateSerial()
        {
            lock(SerialLock)
            {
                ++NextRequestSerial;
                RequestSerial = NextRequestSerial;
            }
        }

        public MemoryStream GetResponseData()
        {
            if(!_eventDone.Wait(60_000))
            {
                throw new Exception("Server responses timeout");
            }

            if (string.IsNullOrEmpty(_exceptionMessage))
            {
                return _responseData;
            }
            else
            {
                System.Diagnostics.Debug.Assert(_exceptionMessage != null);
                throw new Exception(_exceptionMessage);
            }
        }

        public void SetResponseData(Stream data)
        {
            data.CopyTo(_responseData);
            _responseData.Seek(0, SeekOrigin.Begin);
            _eventDone.Set();
            ResponseSource?.SetResult(_responseData);
        }

        public void SetException(string message)
        {
            _exceptionMessage = message;
            _eventDone.Set();
            ResponseSource?.SetException(new Exception(message));
        }

        internal void Reset(bool forAsync)
        {
            UpdateSerial();
            _eventDone.Reset();
            _exceptionMessage = null;
            _responseData.SetLength(0);
            if (forAsync)
            {
                ResponseSource = new TaskCompletionSource<MemoryStream>();
            }
            else
            {
                ResponseSource = null;
            }
        }

        #region IDisposable
        private bool disposedValue;        

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
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
        #endregion
    }    
}