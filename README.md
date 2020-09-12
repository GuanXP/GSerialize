# About GSerialize

GSerialize is a C# binary serializer based on code generating.

* Support most of primitive types
* Support customized serializable class
* Support Array, List, Dictionary
* High performance (10 times faster than BinaryFormatter）
* Target to .NET Standard 2.0

## Usage

### Serialize primitive types listing bellow
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

#### Sample to serialize primitive types
```C#
    var stream = new MemoryStream();
    var serializer = new Serializer(stream);

    UInt16 un16_1 = 123;
    stream.Seek(0, SeekOrigin.Begin);
    serializer.Serialize(un16_1);
    stream.Seek(0, SeekOrigin.Begin);
    var un16_2 = serializer.Deserialize<UInt16>();
    Debug.Assert(un16_1 == un16_2);

    string str1 = "good idea";
    stream.Seek(0, SeekOrigin.Begin);
    serializer.Serialize(str1);
    stream.Seek(0, SeekOrigin.Begin);
    var str2 = serializer.Deserialize<string>();
    Debug.Assert(str1 == str2);

    float f1 = 1.236f;
    stream.Seek(0, SeekOrigin.Begin);
    serializer.Serialize(f1);
    stream.Seek(0, SeekOrigin.Begin);
    var f2 = serializer.Deserialize<float>();
    Debug.Assert(f1 == f2);

    DateTime dt1 = DateTime.Now;
    stream.Seek(0, SeekOrigin.Begin);
    serializer.Serialize(dt1);
    stream.Seek(0, SeekOrigin.Begin);
    var dt2 = serializer.Deserialize<DateTime>();
    Debug.Assert(dt1 == dt2);

    Guid guid_1 = Guid.NewGuid();
    stream.Seek(0, SeekOrigin.Begin);
    serializer.Serialize(guid_1);
    stream.Seek(0, SeekOrigin.Begin);
    var guid_2 = serializer.Deserialize<Guid>();
    Debug.Assert(guid_1 == guid_2);
```

### Serialize a customized class

#### Define a customized serializable class
The field/property can be one of following
* primitive supported types
* List<T> 
* Dictionary<K,V>
* Enum
* Nullable

#### Sample to define a customized serializable class
```C#
    [GSerializable] //GSerializableAttribute marks this class serializable
    public class OptionalFieldUnit
    {
        public string RequiredField; //required field/property must NOT be null

        [Optional]  //OptionalAttribute marks a field/property optional that means it can be null or not
        public string OptionalField;

        [Ignored] //IgnoredAttribute marks a field/property ignored that means it will never be serialized 
        public string IgnoredField;

        public string ReadOnlyProperty => PrivateField; //readonly field/property will be ignored

        private string PrivateField; //none public field/property will be ignored
    }
```

## Behavior
When method serializer.Serialize<T> called，GSerialize will generate serialization codes for type T if they doesn't exist in memory.

All other customized serializable types in the same assembly will get their generated serialization codes at same time.

The generating process will take a few seconds, the client code can call Serializer.CacheSerialiablesInAssembly to generate all serialization codes before any serializer.Serialize<T> calls.


## Limitations
GSerialize doesn't check references among the class fields, so the customized class must avoid property/field reference cycle otherwise it might get a dead loop while serializing.

GSerialize is not thread safe, so client shall avoid calling identical instance of Serializer from variety threads.

Customized serializable class must provide a default constructor.
