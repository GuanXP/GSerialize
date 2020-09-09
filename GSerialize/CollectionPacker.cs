using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GSerialize
{
    public sealed class CollectionPacker
    {
        public static void WriteList<T>(List<T> list, Serializer serializer)
        {
            serializer.SerializeEnumerable(list);
        }

        public static List<T> ReadList<T>(Serializer serializer)
        {
            return serializer.DeserializeList<T>();
        }

        public static void WriteArray<T>(T[] array, Serializer serializer)
        {
            serializer.SerializeEnumerable(array);
        }

        public static T[] ReadArray<T>(Serializer serializer)
        {
            return serializer.DeserializeList<T>().ToArray();
        }

        public static void WriteDict<K, V>(Dictionary<K, V> dict, Serializer serializer)
        {
            serializer.SerializeDict(dict);
        }

        public static Dictionary<K, V> ReadDict<K, V>(Serializer serializer)
        {
            return serializer.DeserializeDict<K, V>();
        }

        public static void WriteNullable<T>(T? value, Serializer serializer) where T : struct
        {
            serializer.Packer.WriteBool(value.HasValue);
            if (value.HasValue)
            {
                serializer.Serialize<T>(value.Value);
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

        public static void WriteEnum<T>(T value, Serializer serializer) where T : Enum
        {
            serializer.Packer.WriteString(value.ToString());
        }

        public static T ReadEnum<T>(Serializer serializer) where T : struct
        {
            var str = serializer.Packer.ReadString();
            Enum.TryParse(str, out T value);
            return value;
        }
    }
}
