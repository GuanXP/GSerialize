/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.IO;
using System.Threading.Tasks;
using GSerialize;

namespace Test
{
    interface ISerializer
    {
        void Serialize<T>(T value);
        Task SerializeAsync<T>(T value);
        T Deserialize<T>();
        Task<T> DeserializeAsync<T>();
    }

    sealed class S1 : ISerializer
    {
        Serializer _serializer;
        internal S1(Stream stream)
        {
            _serializer = new Serializer(stream);
        }

        public T Deserialize<T>()
        {
            return _serializer.Deserialize<T>();
        }

        public Task<T> DeserializeAsync<T>()
        {
            return _serializer.DeserializeAsync<T>();
        }

        public void Serialize<T>(T value)
        {
            _serializer.Serialize<T>(value);
        }

        public Task SerializeAsync<T>(T value)
        {
            return _serializer.SerializeAsync<T>(value);
        }
    }

    sealed class S2 : ISerializer
    {
        Serializer2 _serializer;
        internal S2(Stream stream)
        {
            _serializer = new Serializer2(stream);
        }

        public T Deserialize<T>()
        {
            return _serializer.Deserialize<T>();
        }

        public Task<T> DeserializeAsync<T>()
        {
            return _serializer.DeserializeAsync<T>();
        }

        public void Serialize<T>(T value)
        {
            _serializer.Serialize<T>(value);
        }

        public Task SerializeAsync<T>(T value)
        {
            return _serializer.SerializeAsync<T>(value);
        }
    }
}