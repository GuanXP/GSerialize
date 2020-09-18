using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GSerialize
{
    public sealed class CollectionPacker
    {
        public static void WriteList<T>(List<T> list, Serializer serializer)
        {
            serializer.WriteEnumerable(list);
        }

        public static Task WriteListAsync<T>(List<T> list, Serializer serializer)
        {
            return serializer.WriteEnumerableAsync(list);
        }

        public static List<T> ReadList<T>(Serializer serializer)
        {
            return serializer.ReadList<T>();
        }

        public static Task<List<T>> ReadListAsync<T>(Serializer serializer)
        {
            return serializer.ReadListAsync<T>();
        }

        public static void WriteArray<T>(T[] array, Serializer serializer)
        {
            serializer.WriteEnumerable(array);
        }

        public static Task WriteArrayAsync<T>(T[] array, Serializer serializer)
        {
            return serializer.WriteEnumerableAsync(array);
        }

        public static T[] ReadArray<T>(Serializer serializer)
        {
            return serializer.ReadList<T>().ToArray();
        }

        public async static Task<T[]> ReadArrayAsync<T>(Serializer serializer)
        {
            return (await serializer.ReadListAsync<T>()).ToArray();
        }

        public static void WriteDict<K, V>(Dictionary<K, V> dict, Serializer serializer)
        {
            serializer.WriteDict(dict);
        }

        public static Task WriteDictAsync<K, V>(Dictionary<K, V> dict, Serializer serializer)
        {
            return serializer.WriteDictAsync(dict);
        }

        public static Dictionary<K, V> ReadDict<K, V>(Serializer serializer)
        {
            return serializer.ReadDict<K, V>();
        }

        public static Task<Dictionary<K, V>> ReadDictAsync<K, V>(Serializer serializer)
        {
            return serializer.ReadDictAsync<K, V>();
        }

        public static void WriteNullable<T>(T? value, Serializer serializer) where T : struct
        {
            serializer.Packer.WriteBool(value.HasValue);
            if (value.HasValue)
            {
                serializer.Serialize<T>(value.Value);
            }
        }

        public static async Task WriteNullableAsync<T>(T? value, Serializer serializer) where T : struct
        {
            await serializer.Packer.WriteBoolAsync(value.HasValue);
            if (value.HasValue)
            {
                await serializer.SerializeAsync<T>(value.Value);
            }
        }

        public static T? ReadNullable<T>(Serializer serializer) where T : struct
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

        public static async Task<T?> ReadNullableAsync<T>(Serializer serializer) where T : struct
        {
            var hasValue = await serializer.Packer.ReadBoolAsync();
            if (hasValue)
            {
                return await serializer.DeserializeAsync<T>();
            }
            else
            {
                return null;
            }
        }

        public static void WriteEnum<T>(T value, Serializer serializer) where T : Enum
        {
            serializer.Packer.WriteString(value.ToString());
        }

        public static Task WriteEnumAsync<T>(T value, Serializer serializer) where T : Enum
        {
            return serializer.Packer.WriteStringAsync(value.ToString());
        }

        public static T ReadEnum<T>(Serializer serializer) where T : struct
        {
            var str = serializer.Packer.ReadString();
            Enum.TryParse(str, out T value);
            return value;
        }

        public static async Task<T> ReadEnumAsync<T>(Serializer serializer) where T : struct
        {
            var str = await serializer.Packer.ReadStringAsync();
            Enum.TryParse(str, out T value);
            return value;
        }
    }
}
