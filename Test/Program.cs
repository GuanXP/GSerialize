/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
using GSerialize;
using System;
using System.IO;

namespace Test
{
    public class Program
    {
        static void Main(string[] args)
        {
            PrimitiveUnit.UnitTest();
            PrimitiveUnit.UnitTest2();
            PrimitiveUnit.UnitTestAsync().Wait();
            PrimitiveUnit.UnitTest2Async().Wait();

            Console.WriteLine($"{DateTime.Now} start generating serialization code for assembly");
            //This is not necessary but can accelerate following serializing
            Serializer.PrepareForAssembly(typeof(NestedUnit).Assembly);
            Serializer2.PrepareForAssembly(typeof(NestedUnit).Assembly);
            Console.WriteLine($"{DateTime.Now} end generating serialization code for assembly");

            OptionalFieldUnit.UnitTest();
            OptionalFieldUnit.UnitTest2();
            OptionalFieldUnit.UnitTestAsync().Wait();
            OptionalFieldUnit.UnitTest2Async().Wait();

            CollectionUnit.UnitTest();
            CollectionUnit.UnitTest2();
            CollectionUnit.UnitTestAsync().Wait();
            CollectionUnit.UnitTest2Async().Wait();

            RefUnit.UnitTest();

            PerformanceTest();

            Console.WriteLine($"{DateTime.Now} Unit test passed");            
        }

        private static void PerformanceTest()
        {
            using var mem = new MemoryStream();
            var serializer = new Serializer(mem);

            var p1 = new CollectionUnit();
            Console.WriteLine($"{DateTime.Now}.{DateTime.Now.Millisecond} Serializer: start performance testing");
            for (var i=0; i<100000; ++i)
            {
                mem.Seek(0, SeekOrigin.Begin);
                serializer.Serialize(p1);
                mem.Seek(0, SeekOrigin.Begin);
                serializer.Deserialize<CollectionUnit>();
            }
            Console.WriteLine($"{DateTime.Now}.{DateTime.Now.Millisecond} Serializer: end performance testing");

            Console.WriteLine($"{DateTime.Now}.{DateTime.Now.Millisecond} Serializer2: start performance testing");
            var serializer2 = new Serializer2(mem);
            for (var i=0; i<100000; ++i)
            {
                mem.Seek(0, SeekOrigin.Begin);
                serializer2.Serialize(p1);
                mem.Seek(0, SeekOrigin.Begin);
                serializer2.Deserialize<CollectionUnit>();
            }
            Console.WriteLine($"{DateTime.Now}.{DateTime.Now.Millisecond} Serializer2: end performance testing");

            Console.WriteLine($"{DateTime.Now} Serializer: start performance comparing");
            for (var i = 0; i < 1000000; ++i)
            {
                mem.Seek(0, SeekOrigin.Begin);
                serializer.Serialize(p1.NestedSerializable);
                mem.Seek(0, SeekOrigin.Begin);
                serializer.Deserialize<NestedUnit>();
            }
            Console.WriteLine($"{DateTime.Now} Serializer: end performance comparing");

            Console.WriteLine($"{DateTime.Now} Serializer2: start performance comparing");  
            for (var i = 0; i < 1000000; ++i)
            {
                mem.Seek(0, SeekOrigin.Begin);
                serializer2.Serialize(p1.NestedSerializable);
                mem.Seek(0, SeekOrigin.Begin);
                serializer2.Deserialize<NestedUnit>();
            }
            Console.WriteLine($"{DateTime.Now} Serializer2: end performance comparing");

            Console.WriteLine($"{DateTime.Now} BinaryFormatter: start performance comparing");
            var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            for (var i = 0; i < 1000000; ++i)
            {
                mem.Seek(0, SeekOrigin.Begin);
                formatter.Serialize(mem, p1.NestedSerializable);
                mem.Seek(0, SeekOrigin.Begin);
                formatter.Deserialize(mem);
            }
            Console.WriteLine($"{DateTime.Now} BinaryFormatter: end performance comparing");
        }
    }
}
