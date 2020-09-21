
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using GSerialize;

namespace Test
{
    public class PrimitiveUnit
    {
        private static void UnitTest<TSerializer>(
            TSerializer serializer,
            MemoryStream mem) where TSerializer: ISerializer
        {
            Console.WriteLine($"{DateTime.Now} {typeof(TSerializer).Name}: start testing built-in types");
            Int16 n16_1 = -123;
            mem.Seek(0, SeekOrigin.Begin);
            serializer.Serialize(n16_1);
            mem.Seek(0, SeekOrigin.Begin);
            var n16_2 = serializer.Deserialize<Int16>();
            Debug.Assert(n16_1 == n16_2);

            UInt16 un16_1 = 123;
            mem.Seek(0, SeekOrigin.Begin);
            serializer.Serialize(un16_1);
            mem.Seek(0, SeekOrigin.Begin);
            var un16_2 = serializer.Deserialize<UInt16>();
            Debug.Assert(un16_1 == un16_2);

            Int32 n32_1 = -123;
            mem.Seek(0, SeekOrigin.Begin);
            serializer.Serialize(n32_1);
            mem.Seek(0, SeekOrigin.Begin);
            var n32_2 = serializer.Deserialize<int>();
            Debug.Assert(n32_1 == n32_2);

            UInt32 un32_1 = 123;
            mem.Seek(0, SeekOrigin.Begin);
            serializer.Serialize(un32_1);
            mem.Seek(0, SeekOrigin.Begin);
            var un32_2 = serializer.Deserialize<UInt32>();
            Debug.Assert(un32_1 == un32_2);

            Byte b1 = 123;
            mem.Seek(0, SeekOrigin.Begin);
            serializer.Serialize(b1);
            mem.Seek(0, SeekOrigin.Begin);
            var b2 = serializer.Deserialize<Byte>();
            Debug.Assert(b1 == b2);

            SByte sb1 = -123;
            mem.Seek(0, SeekOrigin.Begin);
            serializer.Serialize(sb1);
            mem.Seek(0, SeekOrigin.Begin);
            var sb2 = serializer.Deserialize<SByte>();
            Debug.Assert(sb1 == sb2);

            Int64 n64_1 = -123;
            mem.Seek(0, SeekOrigin.Begin);
            serializer.Serialize(n64_1);
            mem.Seek(0, SeekOrigin.Begin);
            var n64_2 = serializer.Deserialize<Int64>();
            Debug.Assert(n64_1 == n64_2);

            UInt64 un64_1 = 123;
            mem.Seek(0, SeekOrigin.Begin);
            serializer.Serialize(un64_1);
            mem.Seek(0, SeekOrigin.Begin);
            var un64_2 = serializer.Deserialize<UInt64>();
            Debug.Assert(n64_1 == n64_2);

            Char c1 = 'A';
            mem.Seek(0, SeekOrigin.Begin);
            serializer.Serialize(c1);
            mem.Seek(0, SeekOrigin.Begin);
            var c2 = serializer.Deserialize<Char>();
            Debug.Assert(c1 == c2);

            decimal dec_1 = 123.45678901234m;
            mem.Seek(0, SeekOrigin.Begin);
            serializer.Serialize(dec_1);
            mem.Seek(0, SeekOrigin.Begin);
            var dec_2 = serializer.Deserialize<decimal>();
            Debug.Assert(dec_1 == dec_2);

            string str1 = "good idea";
            mem.Seek(0, SeekOrigin.Begin);
            serializer.Serialize(str1);
            mem.Seek(0, SeekOrigin.Begin);
            var str2 = serializer.Deserialize<string>();
            Debug.Assert(str1 == str2);

            float f1 = 1.236f;
            mem.Seek(0, SeekOrigin.Begin);
            serializer.Serialize(f1);
            mem.Seek(0, SeekOrigin.Begin);
            var f2 = serializer.Deserialize<float>();
            Debug.Assert(f1 == f2);

            double d1 = 1.236;
            mem.Seek(0, SeekOrigin.Begin);
            serializer.Serialize(d1);
            mem.Seek(0, SeekOrigin.Begin);
            var d2 = serializer.Deserialize<double>();
            Debug.Assert(d1 == d2);

            DateTime dt1 = DateTime.Now;
            mem.Seek(0, SeekOrigin.Begin);
            serializer.Serialize(dt1);
            mem.Seek(0, SeekOrigin.Begin);
            var dt2 = serializer.Deserialize<DateTime>();
            Debug.Assert(dt1 == dt2);

            TimeSpan span1 = new TimeSpan(hours: 1, minutes: 5, seconds: 12);
            mem.Seek(0, SeekOrigin.Begin);
            serializer.Serialize(span1);
            mem.Seek(0, SeekOrigin.Begin);
            var span2 = serializer.Deserialize<TimeSpan>();
            Debug.Assert(span2 == span1);

            Guid guid_1 = Guid.NewGuid();
            mem.Seek(0, SeekOrigin.Begin);
            serializer.Serialize(guid_1);
            mem.Seek(0, SeekOrigin.Begin);
            var guid_2 = serializer.Deserialize<Guid>();
            Debug.Assert(guid_1 == guid_2);

            Console.WriteLine($"{DateTime.Now} {typeof(TSerializer).Name}: end testing built-in types");
        }
        
        private static async Task UnitTestAsync<TSerializer>(
            TSerializer serializer,
            MemoryStream mem) where TSerializer: ISerializer
        {
            Console.WriteLine($"{DateTime.Now} {typeof(TSerializer).Name} async: start testing built-in types");
            Int16 n16_1 = -123;
            mem.Seek(0, SeekOrigin.Begin);
            await serializer.SerializeAsync(n16_1);
            mem.Seek(0, SeekOrigin.Begin);
            var n16_2 = await serializer.DeserializeAsync<Int16>();
            Debug.Assert(n16_1 == n16_2);

            UInt16 un16_1 = 123;
            mem.Seek(0, SeekOrigin.Begin);
            await serializer.SerializeAsync(un16_1);
            mem.Seek(0, SeekOrigin.Begin);
            var un16_2 = await serializer.DeserializeAsync<UInt16>();
            Debug.Assert(un16_1 == un16_2);

            Int32 n32_1 = -123;
            mem.Seek(0, SeekOrigin.Begin);
            await serializer.SerializeAsync(n32_1);
            mem.Seek(0, SeekOrigin.Begin);
            var n32_2 = await serializer.DeserializeAsync<int>();
            Debug.Assert(n32_1 == n32_2);

            UInt32 un32_1 = 123;
            mem.Seek(0, SeekOrigin.Begin);
            await serializer.SerializeAsync(un32_1);
            mem.Seek(0, SeekOrigin.Begin);
            var un32_2 = await serializer.DeserializeAsync<UInt32>();
            Debug.Assert(un32_1 == un32_2);

            Int64 n64_1 = -123;
            mem.Seek(0, SeekOrigin.Begin);
            await serializer.SerializeAsync(n64_1);
            mem.Seek(0, SeekOrigin.Begin);
            var n64_2 = await serializer.DeserializeAsync<Int64>();
            Debug.Assert(n64_1 == n64_2);

            UInt64 un64_1 = 123;
            mem.Seek(0, SeekOrigin.Begin);
            await serializer.SerializeAsync(un64_1);
            mem.Seek(0, SeekOrigin.Begin);
            var un64_2 = await serializer.DeserializeAsync<UInt64>();
            Debug.Assert(n64_1 == n64_2);

            Byte b1 = 123;
            mem.Seek(0, SeekOrigin.Begin);
            await serializer.SerializeAsync(b1);
            mem.Seek(0, SeekOrigin.Begin);
            var b2 = await serializer.DeserializeAsync<Byte>();
            Debug.Assert(b1 == b2);

            SByte sb1 = -123;
            mem.Seek(0, SeekOrigin.Begin);
            await serializer.SerializeAsync(sb1);
            mem.Seek(0, SeekOrigin.Begin);
            var sb2 = await serializer.DeserializeAsync<SByte>();
            Debug.Assert(sb1 == sb2);

            Char c1 = 'A';
            mem.Seek(0, SeekOrigin.Begin);
            await serializer.SerializeAsync(c1);
            mem.Seek(0, SeekOrigin.Begin);
            var c2 = await serializer.DeserializeAsync<Char>();
            Debug.Assert(c1 == c2);

            decimal dec_1 = 123.45678901234m;
            mem.Seek(0, SeekOrigin.Begin);
            await serializer.SerializeAsync(dec_1);
            mem.Seek(0, SeekOrigin.Begin);
            var dec_2 = await serializer.DeserializeAsync<decimal>();
            Debug.Assert(dec_1 == dec_2);

            string str1 = "good idea";
            mem.Seek(0, SeekOrigin.Begin);
            await serializer.SerializeAsync(str1);
            mem.Seek(0, SeekOrigin.Begin);
            var str2 = await serializer.DeserializeAsync<string>();
            Debug.Assert(str1 == str2);

            float f1 = 1.236f;
            mem.Seek(0, SeekOrigin.Begin);
            await serializer.SerializeAsync(f1);
            mem.Seek(0, SeekOrigin.Begin);
            var f2 = await serializer.DeserializeAsync<float>();
            Debug.Assert(f1 == f2);

            double d1 = 1.236;
            mem.Seek(0, SeekOrigin.Begin);
            await serializer.SerializeAsync(d1);
            mem.Seek(0, SeekOrigin.Begin);
            var d2 = await serializer.DeserializeAsync<double>();
            Debug.Assert(d1 == d2);

            DateTime dt1 = DateTime.Now;
            mem.Seek(0, SeekOrigin.Begin);
            await serializer.SerializeAsync(dt1);
            mem.Seek(0, SeekOrigin.Begin);
            var dt2 = await serializer.DeserializeAsync<DateTime>();
            Debug.Assert(dt1 == dt2);

            Guid guid_1 = Guid.NewGuid();
            mem.Seek(0, SeekOrigin.Begin);
            await serializer.SerializeAsync(guid_1);
            mem.Seek(0, SeekOrigin.Begin);
            var guid_2 = await serializer.DeserializeAsync<Guid>();
            Debug.Assert(guid_1 == guid_2);

            Console.WriteLine($"{DateTime.Now} {typeof(TSerializer).Name} async: end testing built-in types");
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