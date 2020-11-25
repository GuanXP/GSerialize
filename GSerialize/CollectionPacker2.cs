/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GSerialize
{
    public sealed class CollectionPacker2
    {
        public static void WriteString(
            string value, Serializer2 serializer,
            Dictionary<Object, int> cache)
        {
            serializer.WriteString(value, cache);
        }

        public static Task WriteStringAsync(
            string value, Serializer2 serializer,
            Dictionary<Object, int> cache,
            CancellationToken cancellation)
        {
            return serializer.WriteStringAsync(value, cache, cancellation);
        }

        public static string ReadString(Serializer2 serializer, Dictionary<int, Object> cache)
        {
            return serializer.ReadString(cache);
        }

        public static Task<string> ReadStringAsync(
            Serializer2 serializer, 
            Dictionary<int, Object> cache,
            CancellationToken cancellation)
        {
            return serializer.ReadStringAsync(cache, cancellation);
        }

        public static void WriteList<T>(
            List<T> list, Serializer2 serializer,
            Dictionary<Object, int> cache)
        {
            serializer.WriteEnumerable(list, cache);
        }

        public static Task WriteListAsync<T>(
            List<T> list, Serializer2 serializer,
            Dictionary<Object, int> cache,
            CancellationToken cancellation)
        {
            return serializer.WriteEnumerableAsync(list, cache, cancellation);
        }

        public static List<T> ReadList<T>(Serializer2 serializer, Dictionary<int, Object> cache)
        {
            return serializer.ReadList<T>(cache);
        }

        public static Task<List<T>> ReadListAsync<T>(
            Serializer2 serializer, 
            Dictionary<int, Object> cache,
            CancellationToken cancellation)
        {
            return serializer.ReadListAsync<T>(cache, cancellation);
        }

        public static void WriteArray<T>(
            T[] array, 
            Serializer2 serializer,
            Dictionary<Object, int> cache)
        {
            serializer.WriteEnumerable(array, cache);
        }

        public static Task WriteArrayAsync<T>(
            T[] array, Serializer2 serializer,
            Dictionary<Object, int> cache,
            CancellationToken cancellation)
        {
            return serializer.WriteEnumerableAsync(array, cache, cancellation);
        }

        public static T[] ReadArray<T>(Serializer2 serializer, Dictionary<int, Object> cache)
        {
            return serializer.ReadArray<T>(cache);
        }

        public async static Task<T[]> ReadArrayAsync<T>(
            Serializer2 serializer, 
            Dictionary<int, Object> cache,
            CancellationToken cancellation)
        {
            return await serializer.ReadArrayAsync<T>(cache, cancellation);
        }

        public static void WriteDict<K, V>(
            Dictionary<K, V> value, 
            Serializer2 serializer,
            Dictionary<Object, int> cache)
        {
            serializer.WriteDict(value, cache);
        }

        public static Task WriteDictAsync<K, V>(
            Dictionary<K, V> value, 
            Serializer2 serializer,
            Dictionary<Object, int> cache,
            CancellationToken cancellation)
        {
            return serializer.WriteDictAsync(value, cache, cancellation);
        }

        public static Dictionary<K, V> ReadDict<K, V>(
            Serializer2 serializer, 
            Dictionary<int, Object> cache)
        {
            return serializer.ReadDict<K, V>(cache);
        }

        public static Task<Dictionary<K, V>> ReadDictAsync<K, V>(
            Serializer2 serializer, 
            Dictionary<int, Object> cache,
            CancellationToken cancellation)
        {
            return serializer.ReadDictAsync<K, V>(cache,  cancellation);
        }

        public static void WriteNullable<T>(T? value, Serializer2 serializer) where T : struct
        {
            serializer.Packer.WriteBool(value.HasValue);
            if (value.HasValue)
            {
                serializer.Serialize<T>(value.Value);
            }
        }

        public static async Task WriteNullableAsync<T>(
            T? value, Serializer2 serializer,
            CancellationToken cancellation) where T : struct
        {
            await serializer.Packer.WriteBoolAsync(value.HasValue, cancellation);
            if (value.HasValue)
            {
                await serializer.SerializeAsync<T>(value.Value, cancellation);
            }
        }

        public static T? ReadNullable<T>(Serializer2 serializer) where T : struct
        {
            var hasValue = serializer.Packer.ReadBool();
            if (hasValue)
            {
                return serializer.Deserialize<T>();
            }
            else
            {
                return null;
            }
        }

        public static async Task<T?> ReadNullableAsync<T>(
            Serializer2 serializer,
            CancellationToken cancellation) where T : struct
        {
            var hasValue = await serializer.Packer.ReadBoolAsync(cancellation);
            if (hasValue)
            {
                return await serializer.DeserializeAsync<T>(cancellation);
            }
            else
            {
                return null;
            }
        }

        public static void WriteEnum<T>(T value, Serializer2 serializer) where T : Enum
        {
            serializer.Packer.WriteString(value.ToString());
        }

        public static Task WriteEnumAsync<T>(
            T value, Serializer2 serializer,
            CancellationToken cancellation) where T : Enum
        {
            return serializer.Packer.WriteStringAsync(value.ToString(), cancellation);
        }

        public static T ReadEnum<T>(Serializer2 serializer) where T : struct
        {
            var str = serializer.Packer.ReadString();
            Enum.TryParse(str, out T value);
            return value;
        }

        public static async Task<T> ReadEnumAsync<T>(
            Serializer2 serializer,
            CancellationToken cancellation) where T : struct
        {
            var str = await serializer.Packer.ReadStringAsync(cancellation);
            Enum.TryParse(str, out T value);
            return value;
        }
    }
}
