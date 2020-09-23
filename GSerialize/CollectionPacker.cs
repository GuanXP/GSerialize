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
    public sealed class CollectionPacker
    {
        public static void WriteList<T>(List<T> list, Serializer serializer)
        {
            serializer.WriteEnumerable(list);
        }

        public static Task WriteListAsync<T>(
            List<T> list, Serializer serializer,
            CancellationToken cancellation)
        {
            return serializer.WriteEnumerableAsync(list, cancellation);
        }

        public static List<T> ReadList<T>(Serializer serializer)
        {
            return serializer.ReadList<T>();
        }

        public static Task<List<T>> ReadListAsync<T>(Serializer serializer, CancellationToken cancellation)
        {
            return serializer.ReadListAsync<T>(cancellation);
        }

        public static void WriteArray<T>(T[] array, Serializer serializer)
        {
            serializer.WriteEnumerable(array);
        }

        public static Task WriteArrayAsync<T>(
            T[] array, Serializer serializer, 
            CancellationToken cancellation)
        {
            return serializer.WriteEnumerableAsync(array, cancellation);
        }

        public static T[] ReadArray<T>(Serializer serializer)
        {
            return serializer.ReadList<T>().ToArray();
        }

        public async static Task<T[]> ReadArrayAsync<T>(Serializer serializer, CancellationToken cancellation)
        {
            return (await serializer.ReadListAsync<T>(cancellation)).ToArray();
        }

        public static void WriteDict<K, V>(Dictionary<K, V> dict, Serializer serializer)
        {
            serializer.WriteDict(dict);
        }

        public static Task WriteDictAsync<K, V>(
            Dictionary<K, V> dict, Serializer serializer,
            CancellationToken cancellation)
        {
            return serializer.WriteDictAsync(dict, cancellation);
        }

        public static Dictionary<K, V> ReadDict<K, V>(Serializer serializer)
        {
            return serializer.ReadDict<K, V>();
        }

        public static Task<Dictionary<K, V>> ReadDictAsync<K, V>(
            Serializer serializer, CancellationToken cancellation)
        {
            return serializer.ReadDictAsync<K, V>(cancellation);
        }

        public static void WriteNullable<T>(T? value, Serializer serializer) where T : struct
        {
            System.Diagnostics.Debug.Assert(value.HasValue);
            serializer.Serialize<T>(value.Value);
        }

        public static async Task WriteNullableAsync<T>(
            T? value, Serializer serializer,
            CancellationToken cancellation) where T : struct
        {
            System.Diagnostics.Debug.Assert(value.HasValue);
            await serializer.SerializeAsync<T>(value.Value, cancellation);
        }

        public static T? ReadNullable<T>(Serializer serializer) where T : struct
        {
            return serializer.Deserialize<T>();
        }

        public static async Task<T?> ReadNullableAsync<T>(
            Serializer serializer,
            CancellationToken cancellation) where T : struct
        {
            return await serializer.DeserializeAsync<T>(cancellation);
        }

        public static void WriteEnum<T>(T value, Serializer serializer) where T : Enum
        {
            serializer.Packer.WriteString(value.ToString());
        }

        public static Task WriteEnumAsync<T>(
            T value, Serializer serializer,
            CancellationToken cancellation) where T : Enum
        {
            return serializer.Packer.WriteStringAsync(value.ToString(), cancellation);
        }

        public static T ReadEnum<T>(Serializer serializer) where T : struct
        {
            var str = serializer.Packer.ReadString();
            Enum.TryParse(str, out T value);
            return value;
        }

        public static async Task<T> ReadEnumAsync<T>(
            Serializer serializer,
            CancellationToken cancellation) where T : struct
        {
            var str = await serializer.Packer.ReadStringAsync(cancellation);
            Enum.TryParse(str, out T value);
            return value;
        }
    }
}
