# 关于GSerialize

GSerialize 是基于代码生成的C#二进制序列化方案
* 支持绝大多数原生类型
* 支持用GSerializableAttribute标记的public class
* 支持数组、List、Dictionary等集合类型
* 快速（比 BinaryFormatter 快一个数量级）
* 支持 .NET Standard 2.0

## 用法

### 序列化原生类型，目前支持的原生类型有
* Int16/UInt16
* Int32/UInt32
* Int64/UInt64
* String
* Char
* Decimal
* Float
* Double
* DateTime
* Guid

#### 代码示例
    var mem = new MemoryStream();
    var serializer = new Serializer(mem);

    UInt16 un16_1 = 123;
    mem.Seek(0, SeekOrigin.Begin);
    serializer.Serialize(un16_1);
    mem.Seek(0, SeekOrigin.Begin);
    var un16_2 = serializer.Deserialize<UInt16>();
    Debug.Assert(un16_1 == un16_2);

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

### 自定义类型

#### 定义自己的可序列化类型
自定义类型的字段/属性可以是一下类型
* 原生类型
* List<T> 
* Dictionary<K,V>
* Enum
* Nullable

#### 代码示例
    [GSerializable] //附加 GSerializableAttribute, 标记此类为可序列化
    public class OptionalFieldUnit
    {
        public string RequiredField; //必备字段/属性，必须不为null

        [Optional]  //Optional 标记可选字段/属性，可以为null，也可以不为null
        public string OptionalField;

        [Ignored] //Optional 标记忽略字段/属性，不会被序列化
        public string IgnoredField;

        public string ReadOnlyProperty => PrivateField; //readonly 字段/属性不会被序列化

        private string PrivateField; //private 字段/属性不会被序列化
    }

## 程序行为
当serializer.Serialize<T>方法被调用时，如果针对类型T的序列化代码不存在，则自动生成序列化代码并缓存在内存中，后续则直接调用生成的代码来执行序列化。

当生成类型T相关的序列化代码时，其所在Assembly中的全部GSerializable标记public class皆同时生成序列化代码，且其依赖的类型相关序列化代码也会生成。

生成代码过程相对较慢，可能会消耗若干秒。客户程序可以通过调用Serializer.CacheSerialiablesInAssembly方法来预先生成某个Assembly中全部GSerializable标记的public class相关序列化代码。

## 限制
GSerialize 不支持循环引用检测，若待序列化class中含有循环引用，则序列化过程会死循环，因此自定义类型必须保证不能出现内部成员属性/字段间的交叉引用。

GSerialize 不是线程安全的，从多个线程同时访问GSerialize的同一个实例可能会出错。
