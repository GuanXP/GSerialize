/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GSerialize
{
    public sealed class CollectionPacker2
    {
        public static void WriteString(
            string value, Serializer2 Serializer2,
            Dictionary<Object, int> cache)
        {
            Serializer2.WriteString(value, cache);
        }

        public static Task WriteStringAsync(
            string value, Serializer2 Serializer2,
            Dictionary<Object, int> cache)
        {
            return Serializer2.WriteStringAsync(value, cache);
        }

        public static string ReadString(Serializer2 Serializer2, Dictionary<int, Object> cache)
        {
            return Serializer2.ReadString(cache);
        }

        public static Task<string> ReadStringAsync(Serializer2 Serializer2, Dictionary<int, Object> cache)
        {
            return Serializer2.ReadStringAsync(cache);
        }

        public static void WriteList<T>(
            List<T> list, Serializer2 Serializer2,
            Dictionary<Object, int> cache)
        {
            Serializer2.WriteEnumerable(list, cache);
        }

        public static Task WriteListAsync<T>(
            List<T> list, Serializer2 Serializer2,
            Dictionary<Object, int> cache)
        {
            return Serializer2.WriteEnumerableAsync(list, cache);
        }

        public static List<T> ReadList<T>(Serializer2 Serializer2, Dictionary<int, Object> cache)
        {
            return Serializer2.ReadList<T>(cache);
        }

        public static Task<List<T>> ReadListAsync<T>(Serializer2 Serializer2, Dictionary<int, Object> cache)
        {
            return Serializer2.ReadListAsync<T>(cache);
        }

        public static void WriteArray<T>(
            T[] array, 
            Serializer2 Serializer2,
            Dictionary<Object, int> cache)
        {
            Serializer2.WriteEnumerable(array, cache);
        }

        public static Task WriteArrayAsync<T>(
            T[] array, Serializer2 Serializer2,
            Dictionary<Object, int> cache)
        {
            return Serializer2.WriteEnumerableAsync(array, cache);
        }

        public static T[] ReadArray<T>(Serializer2 Serializer2, Dictionary<int, Object> cache)
        {
            return Serializer2.ReadArray<T>(cache);
        }

        public async static Task<T[]> ReadArrayAsync<T>(
            Serializer2 Serializer2, 
            Dictionary<int, Object> cache)
        {
            return await Serializer2.ReadArrayAsync<T>(cache);
        }

        public static void WriteDict<K, V>(
            Dictionary<K, V> dict, Serializer2 Serializer2,
            Dictionary<Object, int> cache)
        {
            Serializer2.WriteDict(dict, cache);
        }

        public static Task WriteDictAsync<K, V>(
            Dictionary<K, V> dict, Serializer2 Serializer2,
            Dictionary<Object, int> cache)
        {
            return Serializer2.WriteDictAsync(dict, cache);
        }

        public static Dictionary<K, V> ReadDict<K, V>(
            Serializer2 Serializer2, 
            Dictionary<int, Object> cache)
        {
            return Serializer2.ReadDict<K, V>(cache);
        }

        public static Task<Dictionary<K, V>> ReadDictAsync<K, V>(
            Serializer2 Serializer2, 
            Dictionary<int, Object> cache)
        {
            return Serializer2.ReadDictAsync<K, V>(cache);
        }

        public static void WriteNullable<T>(T? value, Serializer2 Serializer2) where T : struct
        {
            Serializer2.Packer.WriteBool(value.HasValue);
            if (value.HasValue)
            {
                Serializer2.Serialize<T>(value.Value);
            }
        }

        public static async Task WriteNullableAsync<T>(T? value, Serializer2 Serializer2) where T : struct
        {
            await Serializer2.Packer.WriteBoolAsync(value.HasValue);
            if (value.HasValue)
            {
                await Serializer2.SerializeAsync<T>(value.Value);
            }
        }

        public static T? ReadNullable<T>(Serializer2 Serializer2) where T : struct
        {
            var hasValue = Serializer2.Packer.ReadBool();
            if (hasValue)
            {
                return Serializer2.Deserialize<T>();
            }
            else
            {
                return null;
            }
        }

        public static async Task<T?> ReadNullableAsync<T>(Serializer2 Serializer2) where T : struct
        {
            var hasValue = await Serializer2.Packer.ReadBoolAsync();
            if (hasValue)
            {
                return await Serializer2.DeserializeAsync<T>();
            }
            else
            {
                return null;
            }
        }

        public static void WriteEnum<T>(T value, Serializer2 Serializer2) where T : Enum
        {
            Serializer2.Packer.WriteString(value.ToString());
        }

        public static Task WriteEnumAsync<T>(T value, Serializer2 Serializer2) where T : Enum
        {
            return Serializer2.Packer.WriteStringAsync(value.ToString());
        }

        public static T ReadEnum<T>(Serializer2 Serializer2) where T : struct
        {
            var str = Serializer2.Packer.ReadString();
            Enum.TryParse(str, out T value);
            return value;
        }

        public static async Task<T> ReadEnumAsync<T>(Serializer2 Serializer2) where T : struct
        {
            var str = await Serializer2.Packer.ReadStringAsync();
            Enum.TryParse(str, out T value);
            return value;
        }
    }
}
