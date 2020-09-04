using GSerialize;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Test
{
    public class PrimitiveTypesUnit
    {
        public static void UnitTest()
        {
            Console.WriteLine($"{DateTime.Now} 基本类型测试开始");
            var mem = new MemoryStream();
            var serializer = new Serializer(mem);

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

            Guid guid_1 = Guid.NewGuid();
            mem.Seek(0, SeekOrigin.Begin);
            serializer.Serialize(guid_1);
            mem.Seek(0, SeekOrigin.Begin);
            var guid_2 = serializer.Deserialize<Guid>();
            Debug.Assert(guid_1 == guid_2);

            Console.WriteLine($"{DateTime.Now} 基本类型测试结束");
        }
    }

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

        public static void UnitTest()
        {
            Console.WriteLine($"{DateTime.Now} 可选字段/属性测试开始");
            var mem = new MemoryStream();
            var serializer = new Serializer(mem);

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
            Debug.Assert(excepted); //必选字段/属性不能为null

            item1.RequiredField = "hello";
            item1.FullAccessibleProperty = "property";
            mem.Seek(0, SeekOrigin.Begin);
            serializer.Serialize(item1);
            mem.Seek(0, SeekOrigin.Begin);
            var item2 = serializer.Deserialize<OptionalFieldUnit>();
            Debug.Assert(item1.RequiredField == item2.RequiredField);
            Debug.Assert(item1.FullAccessibleProperty == item2.FullAccessibleProperty);
            Debug.Assert(item2.OptionalField == null);  //optional字段/属性为null时，反序列化还原为null         
            Debug.Assert(item2.IgnoredField == null);   //ignored字段/属性不会被序列化
            Debug.Assert(item2.PrivateField == null);   //private字段/属性不会被序列化
            Debug.Assert(item2.ReadOnlyProperty == null); //readonly字段/属性不会被序列化

            item1.OptionalField = "now";
            item1.IgnoredField = "Ignored";
            item1.PrivateField = "Private";
            mem.Seek(0, SeekOrigin.Begin);
            serializer.Serialize(item1);
            mem.Seek(0, SeekOrigin.Begin);
            item2 = serializer.Deserialize<OptionalFieldUnit>();
            Debug.Assert(item1.OptionalField == item2.OptionalField);   //optional字段/属性不为null时，反序列化正确还原
            Debug.Assert(item2.IgnoredField == null);                   //ignored字段/属性不会被序列化
            Debug.Assert(item2.PrivateField == null);                   //private字段/属性不会被序列化
            Debug.Assert(item2.ReadOnlyProperty == null);               //readonly字段/属性不会被序列化

            Console.WriteLine($"{DateTime.Now} 可选字段/属性测试结束");
        }
    }

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

        public static void UnitTest()
        {
            Console.WriteLine($"{DateTime.Now} 集合/枚举类型测试开始");
            var mem = new MemoryStream();
            var serializer = new Serializer(mem);

            var p1 = new CollectionUnit { EnumField = ColorEnum.Green };
            p1.NestedSerializable.DoubleValue = 0.7662;
            p1.NestedSerializable.FloatValue = 2.335f;
            p1.NestedSerializable.ListValue.Add(234);
            p1.NestedSerializable.DictValue[789] = "Cool";

            mem.Seek(0, SeekOrigin.Begin);
            serializer.Serialize(p1);
            mem.Seek(0, SeekOrigin.Begin);
            var p2 = serializer.Deserialize<CollectionUnit>();
            Debug.Assert(p2.EnumField == p1.EnumField);
            Debug.Assert(p2.FlagField == p1.FlagField);
            Debug.Assert(!p2.NullableField.HasValue);
            Debug.Assert(p2.EmptyArray.Length == 0);

            Debug.Assert(p1.NestedSerializable.Guid == p2.NestedSerializable.Guid);
            Debug.Assert(p1.NestedSerializable.DoubleValue == p2.NestedSerializable.DoubleValue);
            Debug.Assert(p1.NestedSerializable.FloatValue == p2.NestedSerializable.FloatValue);
            Debug.Assert(p1.NestedSerializable.DateTimeValue == p2.NestedSerializable.DateTimeValue);
            Debug.Assert(p2.NestedSerializable.ListValue.Last() == 234);
            Debug.Assert(p2.NestedSerializable.DictValue[789] == p1.NestedSerializable.DictValue[789]);

            p1.NullableField = 56789;
            p1.DictField.Add("First", new NestedUnit { FloatValue = 556 });
            p1.FlagField = FileModeEnum.Read | FileModeEnum.Write;
            mem.Seek(0, SeekOrigin.Begin);
            serializer.Serialize(p1);
            mem.Seek(0, SeekOrigin.Begin);
            p2 = serializer.Deserialize<CollectionUnit>();
            Debug.Assert(p2.ArrayField[0] == p1.ArrayField[0]);
            Debug.Assert(p2.NullableField.Value == 56789);
            Debug.Assert(p2.DictField["First"].FloatValue == 556);
            Debug.Assert(p2.FlagField == p1.FlagField);

            Console.WriteLine($"{DateTime.Now} 集合/枚举类型测试结束");
        }
    }

    [GSerializable]
    [Serializable]
    public class NestedUnit
    {
        public Guid Guid { get; set; } = Guid.NewGuid();
        public double DoubleValue = 1.00;
        public float FloatValue = 0.57f;
        public List<Int32> ListValue = new List<int> { 1, 2, 3, 4 };
        public DateTime DateTimeValue = DateTime.Now;
        public Dictionary<int, string> DictValue = new Dictionary<int, string>
        {
            { 123, "good" },
            { 456, "hello" }
        };
    }

    public class Program
    {
        static void Main(string[] args)
        {
            PrimitiveTypesUnit.UnitTest();

            Console.WriteLine($"{DateTime.Now} 缓存Assembly生成代码开始");
            //缓存assembly并非必要，但提前缓存将加速后续的序列化操作
            Serializer.CacheSerialiablesInAssembly(typeof(NestedUnit).Assembly);
            Console.WriteLine($"{DateTime.Now} 缓存Assembly生成代码结束");

            OptionalFieldUnit.UnitTest();
            CollectionUnit.UnitTest();
            PerformanceTest();

            Console.WriteLine($"{DateTime.Now} 单元测试通过");
        }

        private static void PerformanceTest()
        {
            var mem = new MemoryStream();
            var serializer = new Serializer(mem);

            var p1 = new CollectionUnit();
            Console.WriteLine($"{DateTime.Now}.{DateTime.Now.Millisecond} 性能测试开始");
            for (var i=0; i<100000; ++i)
            {
                mem.Seek(0, SeekOrigin.Begin);
                serializer.Serialize(p1);
                mem.Seek(0, SeekOrigin.Begin);
                serializer.Deserialize<CollectionUnit>();
            }
            Console.WriteLine($"{DateTime.Now}.{DateTime.Now.Millisecond} 性能测试开始结束");

            Console.WriteLine($"{DateTime.Now} 性能比较GSerialize开始");
            for (var i = 0; i < 1000000; ++i)
            {
                mem.Seek(0, SeekOrigin.Begin);
                serializer.Serialize(p1.NestedSerializable);
                mem.Seek(0, SeekOrigin.Begin);
                serializer.Deserialize<NestedUnit>();
            }
            Console.WriteLine($"{DateTime.Now} 性能比较GSerialize结束");

            Console.WriteLine($"{DateTime.Now} 性能比较BinaryFormatter开始");
            var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            for (var i = 0; i < 1000000; ++i)
            {
                mem.Seek(0, SeekOrigin.Begin);
                formatter.Serialize(mem, p1.NestedSerializable);
                mem.Seek(0, SeekOrigin.Begin);
                formatter.Deserialize(mem);
            }

            Console.WriteLine($"{DateTime.Now} 性能比较BinaryFormatter结束");
        }
    }
}
