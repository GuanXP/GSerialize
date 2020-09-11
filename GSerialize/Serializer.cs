using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace GSerialize
{
    public sealed class Serializer
    {
        private class Methods
        {
            internal MethodInfo Write;
            internal MethodInfo Read;
        }

        private static Dictionary<Type, Methods> TypeMethodsMap = new Dictionary<Type, Methods>();
        private static Dictionary<Type, Methods> PrimitiveTypeMethodsMap = new Dictionary<Type, Methods>();

        static Serializer()
        {
            PrimitiveTypeMethodsMap[typeof(Int32)] = new Methods
            {
                Read = typeof(Packer).GetMethod("ReadInt32"),
                Write = typeof(Packer).GetMethod("WriteInt32"),
            };

            PrimitiveTypeMethodsMap[typeof(UInt32)] = new Methods
            {
                Read = typeof(Packer).GetMethod("ReadUInt32"),
                Write = typeof(Packer).GetMethod("WriteUInt32"),
            };

            PrimitiveTypeMethodsMap[typeof(Int64)] = new Methods
            {
                Read = typeof(Packer).GetMethod("ReadInt64"),
                Write = typeof(Packer).GetMethod("WriteInt64"),
            };

            PrimitiveTypeMethodsMap[typeof(UInt64)] = new Methods
            {
                Read = typeof(Packer).GetMethod("ReadUInt64"),
                Write = typeof(Packer).GetMethod("WriteUInt64"),
            };

            PrimitiveTypeMethodsMap[typeof(Int16)] = new Methods
            {
                Read = typeof(Packer).GetMethod("ReadInt16"),
                Write = typeof(Packer).GetMethod("WriteInt16"),
            };

            PrimitiveTypeMethodsMap[typeof(UInt16)] = new Methods
            {
                Read = typeof(Packer).GetMethod("ReadUInt16"),
                Write = typeof(Packer).GetMethod("WriteUInt16"),
            };

            PrimitiveTypeMethodsMap[typeof(string)] = new Methods
            {
                Read = typeof(Packer).GetMethod("ReadString"),
                Write = typeof(Packer).GetMethod("WriteString"),
            };

            PrimitiveTypeMethodsMap[typeof(Double)] = new Methods
            {
                Read = typeof(Packer).GetMethod("ReadDouble"),
                Write = typeof(Packer).GetMethod("WriteDouble"),
            };

            PrimitiveTypeMethodsMap[typeof(float)] = new Methods
            {
                Read = typeof(Packer).GetMethod("ReadFloat"),
                Write = typeof(Packer).GetMethod("WriteFloat"),
            };

            PrimitiveTypeMethodsMap[typeof(DateTime)] = new Methods
            {
                Read = typeof(Packer).GetMethod("ReadDateTime"),
                Write = typeof(Packer).GetMethod("WriteDateTime"),
            };

            PrimitiveTypeMethodsMap[typeof(Guid)] = new Methods
            {
                Read = typeof(Packer).GetMethod("ReadGuid"),
                Write = typeof(Packer).GetMethod("WriteGuid"),
            };

            PrimitiveTypeMethodsMap[typeof(Char)] = new Methods
            {
                Read = typeof(Packer).GetMethod("ReadChar"),
                Write = typeof(Packer).GetMethod("WriteChar"),
            };

            PrimitiveTypeMethodsMap[typeof(Decimal)] = new Methods
            {
                Read = typeof(Packer).GetMethod("ReadDecimal"),
                Write = typeof(Packer).GetMethod("WriteDecimal"),
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
                types.AddRange(SerializablesInAssembly(a));
            }
            return types;
        }

        static bool IsSerialiableType(Type type)
        {
            return type.IsDefined(typeof(GSerializableAttribute), false) && type.IsPublic;
        }

        private static List<Type> SerializablesInAssembly(Assembly a)
        {
            var serialzables = from t in a.DefinedTypes where IsSerialiableType(t) select t;
            return new List<Type>(serialzables);
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
        /// <typeparam name="T">ther type of object that will be serialized</typeparam>
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

        internal void SerializeEnumerable<T>(IEnumerable<T> value)
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

        internal void SerializeDict<K,V>(Dictionary<K,V> value)
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

        internal Dictionary<K, V> DeserializeDict<K, V>()
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

        internal List<T> DeserializeList<T>()
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

        /// <summary>
        /// Cache an serializable type.
        /// This will cause all serializable types to be cached.
        /// </summary>
        /// <param name="serializableType">the type that will be cached</param>
        /// <exception cref="NotSupportedException">The caching type has no GSerializableAttribute defined</exception>
        public static void CacheSerializable(Type serializableType)
        {
            if (TypeMethodsMap.ContainsKey(serializableType)) return;
            if (!IsSerialiableType(serializableType))
            {
                throw new NotSupportedException($"{serializableType.Name} must be a primitive type or class with GSerializable attribute.");
            }

            CacheSerialiablesInAssembly(serializableType.Assembly);
        }

        /// <summary>
        /// Cache all serializable classes in an assembly.
        /// </summary>
        /// <param name="assembly">The assembly name</param>
        public static void CacheSerialiablesInAssembly(Assembly assembly)
        {
            var generatedAssemblyName = $"gserializ.gen{assembly.GetHashCode()}.dll";
            AddAssembly(assembly, generatedAssemblyName);
        }
    }
}
