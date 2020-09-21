/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using GSerialize;

namespace Test
{
    [GSerializable]
    public class OptionalFieldUnit
    {
        public string RequiredField;

        [Optional]
        public string OptionalField;

        [Ignored]
        public string IgnoredField;

        public string ReadOnlyProperty => PrivateField;
        public string FullAccessibleProperty { get; set; }

        private string PrivateField; 

        private static void UnitTest<TSerializer>(
            TSerializer serializer,
            MemoryStream mem) where TSerializer: ISerializer
        {
            Console.WriteLine($"{DateTime.Now} {typeof(TSerializer).Name}: start testing optional/ignored");
            var item1 = new OptionalFieldUnit();
            
            var excepted = false;
            try
            {
                serializer.Serialize(item1);
            } 
            catch(TargetInvocationException ex)
            {
                excepted = ex.InnerException is ArgumentNullException;
            }
            Debug.Assert(excepted); //required field/property must NOT be null

            item1.RequiredField = "hello";
            item1.FullAccessibleProperty = "property";
            mem.Seek(0, SeekOrigin.Begin);
            serializer.Serialize(item1);
            mem.Seek(0, SeekOrigin.Begin);
            var item2 = serializer.Deserialize<OptionalFieldUnit>();
            Debug.Assert(item1.RequiredField == item2.RequiredField);
            Debug.Assert(item1.FullAccessibleProperty == item2.FullAccessibleProperty);
            Debug.Assert(item2.OptionalField == null);      //optional field/property can be null         
            Debug.Assert(item2.IgnoredField == null);       //ignored field/property will not be serialized
            Debug.Assert(item2.PrivateField == null);       //private field/property will not be serialized
            Debug.Assert(item2.ReadOnlyProperty == null);   //readonly field/property will not be serialized

            item1.OptionalField = "now";
            item1.IgnoredField = "Ignored";
            item1.PrivateField = "Private";
            mem.Seek(0, SeekOrigin.Begin);
            serializer.Serialize(item1);
            mem.Seek(0, SeekOrigin.Begin);
            item2 = serializer.Deserialize<OptionalFieldUnit>();
            Debug.Assert(item1.OptionalField == item2.OptionalField);   //optional field/property can be none null
            Debug.Assert(item2.IgnoredField == null);
            Debug.Assert(item2.PrivateField == null);
            Debug.Assert(item2.ReadOnlyProperty == null);

            Console.WriteLine($"{DateTime.Now} {typeof(TSerializer).Name}: end testing optional/ignored");
        }

        private static async Task UnitTestAsync<TSerializer>(
            TSerializer serializer,
            MemoryStream mem) where TSerializer: ISerializer
        {
            Console.WriteLine($"{DateTime.Now} {typeof(TSerializer).Name} async: start testing optional/ignored");

            var item1 = new OptionalFieldUnit();
            
            var excepted = false;
            try
            {
                await serializer.SerializeAsync(item1);
            } 
            catch(Exception)
            {
                excepted = true;
            }
            Debug.Assert(excepted); //必选字段/属性不能为null

            item1.RequiredField = "hello";
            item1.FullAccessibleProperty = "property";
            mem.Seek(0, SeekOrigin.Begin);
            await serializer.SerializeAsync(item1);
            mem.Seek(0, SeekOrigin.Begin);
            var item2 = await serializer.DeserializeAsync<OptionalFieldUnit>();
            Debug.Assert(item1.RequiredField == item2.RequiredField);
            Debug.Assert(item1.FullAccessibleProperty == item2.FullAccessibleProperty);
            Debug.Assert(item2.OptionalField == null);
            Debug.Assert(item2.IgnoredField == null);
            Debug.Assert(item2.PrivateField == null);
            Debug.Assert(item2.ReadOnlyProperty == null);

            item1.OptionalField = "now";
            item1.IgnoredField = "Ignored";
            item1.PrivateField = "Private";
            mem.Seek(0, SeekOrigin.Begin);
            await serializer.SerializeAsync(item1);
            mem.Seek(0, SeekOrigin.Begin);
            item2 = await serializer.DeserializeAsync<OptionalFieldUnit>();
            Debug.Assert(item1.OptionalField == item2.OptionalField);
            Debug.Assert(item2.IgnoredField == null);
            Debug.Assert(item2.PrivateField == null);
            Debug.Assert(item2.ReadOnlyProperty == null);

            Console.WriteLine($"{DateTime.Now} {typeof(TSerializer).Name} async: end testing optional/ignored");
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