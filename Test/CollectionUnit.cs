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
using System.Linq;
using System.Threading.Tasks;
using GSerialize;

namespace Test
{
    public enum ColorEnum
    {
        Red = 1,
        Green,
        Blue,
    }
    
    [Flags]
    public enum FileModeEnum
    {
        Read = 1,
        Write = 1 << 1
    }

    [GSerializable]
    public class CollectionUnit
    {
        public Int64? NullableField;

        public string[] ArrayField = new string[] { "hello", "world" };
        public Guid[] EmptyArray = new Guid[0];
        public ColorEnum EnumField { set; get; } = ColorEnum.Blue;

        public FileModeEnum FlagField { set; get; } = FileModeEnum.Read;

        public NestedUnit NestedSerializable = new NestedUnit();
        public Dictionary<string, NestedUnit> DictField = new Dictionary<string, NestedUnit>();

        public static void UnitTest<TSerializer>(
            TSerializer serializer,
            MemoryStream mem) where TSerializer: ISerializer
        {
            Console.WriteLine($"{DateTime.Now} {typeof(TSerializer).Name}: start testing collection/enum");

            var p1 = new CollectionUnit { EnumField = ColorEnum.Green };
            p1.NestedSerializable.DoubleField = 0.7662;
            p1.NestedSerializable.FloatField = 2.335f;
            p1.NestedSerializable.ListField.Add(234);
            p1.NestedSerializable.DictField[789] = "Cool";

            mem.Seek(0, SeekOrigin.Begin);
            serializer.Serialize(p1);
            mem.Seek(0, SeekOrigin.Begin);
            var p2 = serializer.Deserialize<CollectionUnit>();
            Debug.Assert(p2.EnumField == p1.EnumField);
            Debug.Assert(p2.FlagField == p1.FlagField);
            Debug.Assert(!p2.NullableField.HasValue);
            Debug.Assert(p2.EmptyArray.Length == 0);

            Debug.Assert(p1.NestedSerializable.Guid == p2.NestedSerializable.Guid);
            Debug.Assert(p1.NestedSerializable.DoubleField == p2.NestedSerializable.DoubleField);
            Debug.Assert(p1.NestedSerializable.FloatField == p2.NestedSerializable.FloatField);
            Debug.Assert(p1.NestedSerializable.DateTimeField == p2.NestedSerializable.DateTimeField);
            Debug.Assert(p2.NestedSerializable.ListField.Last() == 234);
            Debug.Assert(p2.NestedSerializable.DictField[789] == p1.NestedSerializable.DictField[789]);

            p1.NullableField = 56789;
            p1.DictField.Add("First", new NestedUnit { FloatField = 556 });
            p1.FlagField = FileModeEnum.Read | FileModeEnum.Write;
            mem.Seek(0, SeekOrigin.Begin);
            serializer.Serialize(p1);
            mem.Seek(0, SeekOrigin.Begin);
            p2 = serializer.Deserialize<CollectionUnit>();
            Debug.Assert(p2.ArrayField[0] == p1.ArrayField[0]);
            Debug.Assert(p2.NullableField.Value == 56789);
            Debug.Assert(p2.DictField["First"].FloatField == 556);
            Debug.Assert(p2.FlagField == p1.FlagField);

            Console.WriteLine($"{DateTime.Now} {typeof(TSerializer).Name}: end testing collection/enum");
        }

        public static async Task UnitTestAsync<TSerializer>(
            TSerializer serializer,
            MemoryStream mem) where TSerializer: ISerializer
        {
            Console.WriteLine($"{DateTime.Now} {typeof(TSerializer).Name} async: start testing collection/enum");

            var p1 = new CollectionUnit { EnumField = ColorEnum.Green };
            p1.NestedSerializable.DoubleField = 0.7662;
            p1.NestedSerializable.FloatField = 2.335f;
            p1.NestedSerializable.ListField.Add(234);
            p1.NestedSerializable.DictField[789] = "Cool";

            mem.Seek(0, SeekOrigin.Begin);
            await serializer.SerializeAsync(p1);
            mem.Seek(0, SeekOrigin.Begin);
            var p2 = await serializer.DeserializeAsync<CollectionUnit>();
            Debug.Assert(p2.EnumField == p1.EnumField);
            Debug.Assert(p2.FlagField == p1.FlagField);
            Debug.Assert(!p2.NullableField.HasValue);
            Debug.Assert(p2.EmptyArray.Length == 0);

            Debug.Assert(p1.NestedSerializable.Guid == p2.NestedSerializable.Guid);
            Debug.Assert(p1.NestedSerializable.DoubleField == p2.NestedSerializable.DoubleField);
            Debug.Assert(p1.NestedSerializable.FloatField == p2.NestedSerializable.FloatField);
            Debug.Assert(p1.NestedSerializable.DateTimeField == p2.NestedSerializable.DateTimeField);
            Debug.Assert(p2.NestedSerializable.ListField.Last() == 234);
            Debug.Assert(p2.NestedSerializable.DictField[789] == p1.NestedSerializable.DictField[789]);

            p1.NullableField = 56789;
            p1.DictField.Add("First", new NestedUnit { FloatField = 556 });
            p1.FlagField = FileModeEnum.Read | FileModeEnum.Write;
            mem.Seek(0, SeekOrigin.Begin);
            await serializer.SerializeAsync(p1);
            mem.Seek(0, SeekOrigin.Begin);
            p2 = await serializer.DeserializeAsync<CollectionUnit>();
            Debug.Assert(p2.ArrayField[0] == p1.ArrayField[0]);
            Debug.Assert(p2.NullableField.Value == 56789);
            Debug.Assert(p2.DictField["First"].FloatField == 556);
            Debug.Assert(p2.FlagField == p1.FlagField);

            Console.WriteLine($"{DateTime.Now} {typeof(TSerializer).Name} async: end testing collection/enum");
        }

        internal static void UnitTest()
        {
            using var mem = new MemoryStream();
            var serializer = new Serializer(mem);
            UnitTest(serializer, mem);
        }

        internal static Task UnitTestAsync()
        {
            using var mem = new MemoryStream();
            var serializer = new Serializer(mem);
            return UnitTestAsync(serializer, mem);
        }

        internal static void UnitTest2()
        {
            using var mem = new MemoryStream();
            var serializer = new Serializer2(mem);
            UnitTest(serializer, mem);
        }

        internal static Task UnitTest2Async()
        {
            using var mem = new MemoryStream();
            var serializer = new Serializer2(mem);
            return UnitTestAsync(serializer, mem);
        }
    }
}