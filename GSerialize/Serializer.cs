using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace GSerialize
{
    public sealed class Serializer
    {
        private class Methods
        {
            internal MethodInfo Write;
            internal MethodInfo Read;
            internal MethodInfo WriteAsync;
            internal MethodInfo ReadAsync;
        }

        private static Dictionary<Type, Methods> TypeMethodsMap = new Dictionary<Type, Methods>();
        private static Dictionary<Type, Methods> PrimitiveTypeMethodsMap = new Dictionary<Type, Methods>();

        private static bool s_DetectingReference = false;
        public bool DetectingReference
        {
            get => s_DetectingReference;
            set
            {
                if (value != s_DetectingReference)
                {
                    s_DetectingReference = value;
                    PrimitiveTypeMethodsMap = new Dictionary<Type, Methods>(); //Re-generate code later
                }
            }
        }
        static Serializer()
        {
            PrimitiveTypeMethodsMap[typeof(Int32)] = new Methods
            {
                Read = typeof(Packer).GetMethod("ReadInt32"),
                Write = typeof(Packer).GetMethod("WriteInt32"),
                ReadAsync = typeof(Packer).GetMethod("ReadInt32Async"),
                WriteAsync = typeof(Packer).GetMethod("WriteInt32Async"),
            };

            PrimitiveTypeMethodsMap[typeof(UInt32)] = new Methods
            {
                Read = typeof(Packer).GetMethod("ReadUInt32"),
                Write = typeof(Packer).GetMethod("WriteUInt32"),
                ReadAsync = typeof(Packer).GetMethod("ReadUInt32Async"),
                WriteAsync = typeof(Packer).GetMethod("WriteUInt32Async"),
            };

            PrimitiveTypeMethodsMap[typeof(Int64)] = new Methods
            {
                Read = typeof(Packer).GetMethod("ReadInt64"),
                Write = typeof(Packer).GetMethod("WriteInt64"),
                ReadAsync = typeof(Packer).GetMethod("ReadInt64Async"),
                WriteAsync = typeof(Packer).GetMethod("WriteInt64Async"),
            };

            PrimitiveTypeMethodsMap[typeof(UInt64)] = new Methods
            {
                Read = typeof(Packer).GetMethod("ReadUInt64"),
                Write = typeof(Packer).GetMethod("WriteUInt64"),
                ReadAsync = typeof(Packer).GetMethod("ReadUInt64Async"),
                WriteAsync = typeof(Packer).GetMethod("WriteUInt64Async"),
            };

            PrimitiveTypeMethodsMap[typeof(Int16)] = new Methods
            {
                Read = typeof(Packer).GetMethod("ReadInt16"),
                Write = typeof(Packer).GetMethod("WriteInt16"),
                ReadAsync = typeof(Packer).GetMethod("ReadInt16Async"),
                WriteAsync = typeof(Packer).GetMethod("WriteInt16Async"),
            };

            PrimitiveTypeMethodsMap[typeof(UInt16)] = new Methods
            {
                Read = typeof(Packer).GetMethod("ReadUInt16"),
                Write = typeof(Packer).GetMethod("WriteUInt16"),
                ReadAsync = typeof(Packer).GetMethod("ReadUInt16Async"),
                WriteAsync = typeof(Packer).GetMethod("WriteUInt16Async"),
            };

            PrimitiveTypeMethodsMap[typeof(Byte)] = new Methods
            {
                Read = typeof(Packer).GetMethod("ReadByte"),
                Write = typeof(Packer).GetMethod("WriteByte"),
                ReadAsync = typeof(Packer).GetMethod("ReadByteAsync"),
                WriteAsync = typeof(Packer).GetMethod("WriteByteAsync"),
            };

            PrimitiveTypeMethodsMap[typeof(SByte)] = new Methods
            {
                Read = typeof(Packer).GetMethod("ReadSByte"),
                Write = typeof(Packer).GetMethod("WriteSByte"),
                ReadAsync = typeof(Packer).GetMethod("ReadSByteAsync"),
                WriteAsync = typeof(Packer).GetMethod("WriteSByteAsync"),
            };

            PrimitiveTypeMethodsMap[typeof(string)] = new Methods
            {
                Read = typeof(Packer).GetMethod("ReadString"),
                Write = typeof(Packer).GetMethod("WriteString"),
                ReadAsync = typeof(Packer).GetMethod("ReadStringAsync"),
                WriteAsync = typeof(Packer).GetMethod("WriteStringAsync"),
            };

            PrimitiveTypeMethodsMap[typeof(Double)] = new Methods
            {
                Read = typeof(Packer).GetMethod("ReadDouble"),
                Write = typeof(Packer).GetMethod("WriteDouble"),
                ReadAsync = typeof(Packer).GetMethod("ReadDoubleAsync"),
                WriteAsync = typeof(Packer).GetMethod("WriteDoubleAsync"),
            };

            PrimitiveTypeMethodsMap[typeof(float)] = new Methods
            {
                Read = typeof(Packer).GetMethod("ReadFloat"),
                Write = typeof(Packer).GetMethod("WriteFloat"),
                ReadAsync = typeof(Packer).GetMethod("ReadFloatAsync"),
                WriteAsync = typeof(Packer).GetMethod("WriteFloatAsync"),
            };

            PrimitiveTypeMethodsMap[typeof(DateTime)] = new Methods
            {
                Read = typeof(Packer).GetMethod("ReadDateTime"),
                Write = typeof(Packer).GetMethod("WriteDateTime"),
                ReadAsync = typeof(Packer).GetMethod("ReadDateTimeAsync"),
                WriteAsync = typeof(Packer).GetMethod("WriteDateTimeAsync"),
            };

            PrimitiveTypeMethodsMap[typeof(TimeSpan)] = new Methods
            {
                Read = typeof(Packer).GetMethod("ReadTimeSpan"),
                Write = typeof(Packer).GetMethod("WriteTimeSpan"),
                ReadAsync = typeof(Packer).GetMethod("ReadTimeSpanAsync"),
                WriteAsync = typeof(Packer).GetMethod("WriteTimeSpanAsync"),
            };

            PrimitiveTypeMethodsMap[typeof(Guid)] = new Methods
            {
                Read = typeof(Packer).GetMethod("ReadGuid"),
                Write = typeof(Packer).GetMethod("WriteGuid"),
                ReadAsync = typeof(Packer).GetMethod("ReadGuidAsync"),
                WriteAsync = typeof(Packer).GetMethod("WriteGuidAsync"),
            };

            PrimitiveTypeMethodsMap[typeof(Char)] = new Methods
            {
                Read = typeof(Packer).GetMethod("ReadChar"),
                Write = typeof(Packer).GetMethod("WriteChar"),
                ReadAsync = typeof(Packer).GetMethod("ReadCharAsync"),
                WriteAsync = typeof(Packer).GetMethod("WriteCharAsync"),
            };

            PrimitiveTypeMethodsMap[typeof(Decimal)] = new Methods
            {
                Read = typeof(Packer).GetMethod("ReadDecimal"),
                Write = typeof(Packer).GetMethod("WriteDecimal"),
                ReadAsync = typeof(Packer).GetMethod("ReadDecimalAsync"),
                WriteAsync = typeof(Packer).GetMethod("WriteDecimalAsync"),
            };
        }

        private static void AddAssembly(Assembly assembly, string generatedAssemblyName)
        {
            var set = new HashSet<Assembly>();
            GetDepencyAssemblies(assembly, set);
            var referencedAssemblies = new List<Assembly>(set.ToList());
            var serializingTypes = CollectSerializableTypes(referencedAssemblies);
            var firstSerialabeType = serializingTypes.FirstOrDefault();
            if (firstSerialabeType == null || TypeMethodsMap.ContainsKey(firstSerialabeType)) return;

            var mapCopy = new Dictionary<Type, Methods>(TypeMethodsMap);
            var compiledAssembly = CodeGenerator.CompileSerialable(
                serializingTypes, referencedAssemblies, generatedAssemblyName);
            foreach (var t in serializingTypes)
            {
                var classType = compiledAssembly.GetType(t.GeneratedFullClassName());
                var methods = new Methods
                {
                    Write = classType.GetMethod("Write"),
                    Read = classType.GetMethod("Read"),
                    WriteAsync = classType.GetMethod("WriteAsync"),
                    ReadAsync = classType.GetMethod("ReadAsync"),
                };
                mapCopy[t] = methods;
            }
            TypeMethodsMap = mapCopy;
        }

        private static List<Type> CollectSerializableTypes(List<Assembly> assemblies)
        {
            var types = new List<Type>();
            foreach (var a in assemblies)
            {
                types.AddRange(SerializableInAssembly(a));
            }
            return types;
        }

        private static List<Type> SerializableInAssembly(Assembly a)
        {
            var serialzableTypes = from t in a.DefinedTypes 
                where t.IsGSerializable() && !t.IsAbstract && t.IsPublic
                select t;
            return new List<Type>(serialzableTypes);
        }

        private static void GetDepencyAssemblies(Assembly assembly, HashSet<Assembly> assemblies)
        {
            if (!assemblies.Contains(assembly))
            {
                assemblies.Add(assembly);
                foreach(var aName in assembly.GetReferencedAssemblies())
                {
                    GetDepencyAssemblies(Assembly.Load(aName), assemblies);
                }
            }
        }


        public readonly Packer Packer;
        private readonly Object[] _paramsReading;

        /// <summary>
        /// Construct a new Serializer instance
        /// </summary>
        /// <param name="stream">the stream that will take the serialized data</param>
        public Serializer(Stream stream)
        {
            Packer = new Packer(stream);
            _paramsReading = new object[] { this };
        }

        private static void MethodsForType(Type type, out Methods packerMethods, out Methods methods)
        {
            PrimitiveTypeMethodsMap.TryGetValue(type, out packerMethods);
            if (packerMethods != null)
            {
                methods = null;
                return;
            }

            TypeMethodsMap.TryGetValue(type, out methods);
            if (methods == null)
            {
                CacheSerializable(type);
                methods = TypeMethodsMap[type];
            }
        }

        /// <summary>
        /// Serialize an object into a stream
        /// </summary>
        /// <typeparam name="T">the type of object that will be serialized</typeparam>
        /// <param name="value">the object that will be serialized</param>        
        public void Serialize<T>(T value)
        {
            MethodsForType(typeof(T), out Methods packerMethods, out Methods methods);
            if (packerMethods != null)
            {
                packerMethods.Write.Invoke(Packer, new object[] { value });
            }
            else
            {
                methods.Write.Invoke(null, new object[] { value, this });
            }
        }

        /// <summary>
        /// Serialize an object asynchronously into a stream
        /// </summary>
        /// <typeparam name="T">the type of object that will be serialized</typeparam>
        /// <param name="value">the object that will be serialized</param>
        public Task SerializeAsync<T>(T value)
        {
            MethodsForType(typeof(T), out Methods packerMethods, out Methods methods);
            if (packerMethods != null)
            {
                return (Task)packerMethods.WriteAsync.Invoke(Packer, new object[] { value });
            }
            else
            {
                return (Task)methods.WriteAsync.Invoke(null, new object[] { value, this });
            }
        }

        internal void WriteEnumerable<T>(IEnumerable<T> value)
        {
            MethodsForType(typeof(T), out Methods packerMethods, out Methods methods);
            Packer.WriteInt32(value.Count());
            if (packerMethods != null)
            {
                foreach (var item in value)
                {
                    packerMethods.Write.Invoke(Packer, new object[] { item });
                }
            }
            else
            {
                foreach (var item in value)
                {
                    methods.Write.Invoke(null, new object[] { item, this });
                }
            }
        }

        internal async Task WriteEnumerableAsync<T>(IEnumerable<T> value)
        {
            MethodsForType(typeof(T), out Methods packerMethods, out Methods methods);
            await Packer.WriteInt32Async(value.Count());
            if (packerMethods != null)
            {
                foreach (var item in value)
                {
                    await (Task)packerMethods.WriteAsync.Invoke(Packer, new object[] { item });
                }
            }
            else
            {
                foreach (var item in value)
                {
                    await (Task)methods.WriteAsync.Invoke(null, new object[] { item, this });
                }
            }
        }

        internal void WriteDict<K,V>(Dictionary<K,V> value)
        {
            MethodsForType(typeof(K), out Methods packerMethodsK, out Methods methodsK);
            MethodsForType(typeof(V), out Methods packerMethodsV, out Methods methodsV);

            Packer.WriteInt32(value.Count());
            if (packerMethodsK != null && packerMethodsV != null)
            {
                foreach (var item in value)
                {
                    packerMethodsK.Write.Invoke(Packer, new object[] { item.Key });
                    packerMethodsV.Write.Invoke(Packer, new object[] { item.Value });
                }
            }
            else if (packerMethodsK != null && methodsV != null)
            {
                foreach (var item in value)
                {
                    packerMethodsK.Write.Invoke(Packer, new object[] { item.Key });
                    methodsV.Write.Invoke(null, new object[] { item.Value, this });
                }
            }
            else if (methodsK != null && methodsV != null)
            {
                foreach (var item in value)
                {
                    methodsK.Write.Invoke(null, new object[] { item.Key, this });
                    methodsV.Write.Invoke(null, new object[] { item.Value, this });
                }
            }
            else
            {
                foreach (var item in value)
                {
                    methodsK.Write.Invoke(null, new object[] { item.Key, this });
                    packerMethodsV.Write.Invoke(Packer, new object[] { item.Value });
                }
            }
        }

        internal async Task WriteDictAsync<K,V>(Dictionary<K,V> value)
        {
            MethodsForType(typeof(K), out Methods packerMethodsK, out Methods methodsK);
            MethodsForType(typeof(V), out Methods packerMethodsV, out Methods methodsV);

            await Packer.WriteInt32Async(value.Count());
            if (packerMethodsK != null && packerMethodsV != null)
            {
                foreach (var item in value)
                {
                    await (Task)packerMethodsK.WriteAsync.Invoke(Packer, new object[] { item.Key });
                    await (Task)packerMethodsV.WriteAsync.Invoke(Packer, new object[] { item.Value });
                }
            }
            else if (packerMethodsK != null && methodsV != null)
            {
                foreach (var item in value)
                {
                    await (Task)packerMethodsK.WriteAsync.Invoke(Packer, new object[] { item.Key });
                    await (Task)methodsV.WriteAsync.Invoke(null, new object[] { item.Value, this });
                }
            }
            else if (methodsK != null && methodsV != null)
            {
                foreach (var item in value)
                {
                    await (Task)methodsK.WriteAsync.Invoke(null, new object[] { item.Key, this });
                    await (Task)methodsV.WriteAsync.Invoke(null, new object[] { item.Value, this });
                }
            }
            else
            {
                foreach (var item in value)
                {
                    await (Task)methodsK.WriteAsync.Invoke(null, new object[] { item.Key, this });
                    await (Task)packerMethodsV.WriteAsync.Invoke(Packer, new object[] { item.Value });
                }
            }
        }

        internal Dictionary<K, V> ReadDict<K, V>()
        {
            MethodsForType(typeof(K), out Methods packerMethodsK, out Methods methodsK);
            MethodsForType(typeof(V), out Methods packerMethodsV, out Methods methodsV);

            var count = Packer.ReadInt32();
            var dict = new Dictionary<K, V>(capacity: count);
            if (packerMethodsK != null && packerMethodsV != null)
            {
                var args = Array.Empty<Object>();
                for (var i = 0; i < count; ++i)
                {
                    var k = (K)packerMethodsK.Read.Invoke(Packer, args);
                    var v = (V)packerMethodsV.Read.Invoke(Packer, args);
                    dict[k] = v;
                }
            }
            else if (packerMethodsK != null && methodsV != null)
            {
                var args = Array.Empty<Object>();
                for (var i = 0; i < count; ++i)
                {
                    var k = (K)packerMethodsK.Read.Invoke(Packer, args);
                    var v = (V)methodsV.Read.Invoke(null, _paramsReading);
                    dict[k] = v;
                }
            }
            else if (methodsK != null && methodsV != null)
            {
                for (var i = 0; i < count; ++i)
                {
                    var k = (K)methodsK.Read.Invoke(null, _paramsReading);
                    var v = (V)methodsV.Read.Invoke(null, _paramsReading);
                    dict[k] = v;
                }
            }
            else
            {
                var args = Array.Empty<Object>();
                for (var i = 0; i < count; ++i)
                {
                    var k = (K)methodsK.Read.Invoke(null, _paramsReading);
                    var v = (V)packerMethodsV.Read.Invoke(Packer, args);
                    dict[k] = v;
                }
            }
            return dict;
        }

        internal async Task<Dictionary<K, V>> ReadDictAsync<K, V>()
        {
            MethodsForType(typeof(K), out Methods packerMethodsK, out Methods methodsK);
            MethodsForType(typeof(V), out Methods packerMethodsV, out Methods methodsV);

            var count = await Packer.ReadInt32Async();
            var dict = new Dictionary<K, V>(capacity: count);
            if (packerMethodsK != null && packerMethodsV != null)
            {
                var args = Array.Empty<Object>();
                for (var i = 0; i < count; ++i)
                {
                    var k = await (Task<K>)packerMethodsK.ReadAsync.Invoke(Packer, args);
                    var v = await (Task<V>)packerMethodsV.ReadAsync.Invoke(Packer, args);
                    dict[k] = v;
                }
            }
            else if (packerMethodsK != null && methodsV != null)
            {
                var args = Array.Empty<Object>();
                for (var i = 0; i < count; ++i)
                {
                    var k = await (Task<K>)packerMethodsK.ReadAsync.Invoke(Packer, args);
                    var v = await (Task<V>)methodsV.ReadAsync.Invoke(null, _paramsReading);
                    dict[k] = v;
                }
            }
            else if (methodsK != null && methodsV != null)
            {
                for (var i = 0; i < count; ++i)
                {
                    var k = await (Task<K>)methodsK.ReadAsync.Invoke(null, _paramsReading);
                    var v = await (Task<V>)methodsV.ReadAsync.Invoke(null, _paramsReading);
                    dict[k] = v;
                }
            }
            else
            {
                var args = Array.Empty<Object>();
                for (var i = 0; i < count; ++i)
                {
                    var k = await (Task<K>)methodsK.ReadAsync.Invoke(null, _paramsReading);
                    var v = await (Task<V>)packerMethodsV.ReadAsync.Invoke(Packer, args);
                    dict[k] = v;
                }
            }
            return dict;
        }


        /// <summary>
        /// deserialize an object as type T from a stream. 
        /// </summary>
        /// <typeparam name="T">The object type that will be deserialize</typeparam>
        /// <returns>The deserialized object</returns>
        /// <exception cref="System.IO.IOException">The input stream closed</exception>
        public T Deserialize<T>()
        {
            MethodsForType(typeof(T), out Methods packerMethods, out Methods methods);
            if (packerMethods != null)
            {
                return (T)packerMethods.Read.Invoke(Packer, Array.Empty<Object>());
            }
            else
            {
                return (T)methods.Read.Invoke(null, _paramsReading);
            }
        }

        /// <summary>
        /// deserialize an object asynchronously as type T from a stream. 
        /// </summary>
        /// <typeparam name="T">The object type that will be deserialize</typeparam>
        /// <returns>The deserialized object</returns>
        /// <exception cref="System.IO.IOException">The input stream closed</exception>
        public Task<T> DeserializeAsync<T>()
        {
            MethodsForType(typeof(T), out Methods packerMethods, out Methods methods);
            if (packerMethods != null)
            {
                return (Task<T>)packerMethods.ReadAsync.Invoke(Packer, Array.Empty<Object>());
            }
            else
            {
                return (Task<T>)methods.ReadAsync.Invoke(null, _paramsReading);
            }
        }

        internal List<T> ReadList<T>()
        {
            MethodsForType(typeof(T), out Methods packerMethods, out Methods methods);
            var type = typeof(T);
            var count = Packer.ReadInt32();
            var list = new List<T>(capacity: count);

            if (packerMethods != null)
            {
                var args = Array.Empty<Object>();
                for (var i = 0; i < count; ++i)
                {
                    list.Add((T)packerMethods.Read.Invoke(Packer, args));
                }
            }
            else
            {
                for (var i = 0; i < count; ++i)
                {
                    list.Add((T)methods.Read.Invoke(null, _paramsReading));
                }
            }
            return list;
        }

        internal async Task<List<T>> ReadListAsync<T>()
        {
            MethodsForType(typeof(T), out Methods packerMethods, out Methods methods);
            var type = typeof(T);
            var count = await Packer.ReadInt32Async();
            var list = new List<T>(capacity: count);

            if (packerMethods != null)
            {
                var args = Array.Empty<Object>();
                for (var i = 0; i < count; ++i)
                {
                    list.Add(await (Task<T>)packerMethods.ReadAsync.Invoke(Packer, args));
                }
            }
            else
            {
                for (var i = 0; i < count; ++i)
                {
                    list.Add(await (Task<T>)methods.ReadAsync.Invoke(null, _paramsReading));
                }
            }
            return list;
        }

        /// <summary>
        /// Cache an serializable type.
        /// This will cause all serializable types to be cached.
        /// </summary>
        /// <param name="serializableType">the type that will be cached</param>
        /// <exception cref="NotSupportedException">The caching type has no GSerializableAttribute defined</exception>
        public static void CacheSerializable(Type serializableType)
        {
            if (TypeMethodsMap.ContainsKey(serializableType)) return;
            if (!serializableType.IsGSerializable())
            {
                throw new NotSupportedException($"{serializableType.Name} must be a primitive type or class with GSerializable attribute.");
            }
            if (serializableType.ContainsGenericParameters)
            {
                throw new NotSupportedException($"{serializableType.FullName} must NOT be a generic type");
            }
            if (serializableType.IsAbstract || !serializableType.IsPublic)
            {
                throw new NotSupportedException($"{serializableType.FullName} must be a non-abstract and public type");
            }

            CacheSerializableInAssembly(serializableType.Assembly);
        }

        /// <summary>
        /// Cache all serializable classes in an assembly.
        /// </summary>
        /// <param name="assembly">The assembly name</param>
        public static void CacheSerializableInAssembly(Assembly assembly)
        {
            var generatedAssemblyName = $"gserialize.gen{assembly.GetHashCode()}.dll";
            AddAssembly(assembly, generatedAssemblyName);
        }
    }
}
