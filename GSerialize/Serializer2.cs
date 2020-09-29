/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace GSerialize
{
    public sealed class Serializer2: ISerializer
    {
        private static Dictionary<Type, SerialMethods> TypeMethodsMap = new Dictionary<Type, SerialMethods>();
        private static void AddAssembly(Assembly assembly, string generatedAssemblyName)
        {
            var referencedAssemblies = DependencyWalker.GetReferencedAssemblies(assembly);
            var serializingTypes = DependencyWalker.CollectSatisfiedType(referencedAssemblies, 
                t=>t.IsSerializableClass());
            var firstSerialabeType = serializingTypes.FirstOrDefault();
            if (firstSerialabeType == null || TypeMethodsMap.ContainsKey(firstSerialabeType)) return;

            var mapCopy = new Dictionary<Type, SerialMethods>(TypeMethodsMap);
            var compiledAssembly = CodeGenerator2.CompileSerialable(
                serializingTypes, referencedAssemblies, generatedAssemblyName);
            foreach (var t in serializingTypes)
            {
                var classType = compiledAssembly.GetType(t.GeneratedFullClassName2());
                //for debugging
                //var classType = Assembly.GetType(t.GeneratedFullClassName2());
                var methods = new SerialMethods
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

        public readonly Packer Packer;
        private readonly object[] _paramsReadingPacker = Array.Empty<Object>();
        private readonly object[] _paramsReadingPackerAsync;
        private readonly object[] _paramsReading;
        private readonly object[] _paramsReadingAsync;
        private readonly object[] _paramsWrittingPacker;
        private readonly object[] _paramsWrittingPackerAsync;
        private readonly object[] _paramsWritting;
        private readonly object[] _paramsWrittingAsync;
        /// <summary>
        /// Construct a new Serializer instance
        /// </summary>
        /// <param name="stream">the stream that will take the serialized data</param>
        public Serializer2(Stream stream)
        {
            Packer = new Packer(stream);
            _paramsReadingPackerAsync = new object[1];
            _paramsReading = new object[] {this, null};
            _paramsReadingAsync = new object[] {this, null, null};
            _paramsWrittingPacker = new object[1];
            _paramsWrittingPackerAsync = new object[2];
            _paramsWritting = new object[] {null, this, null};
            _paramsWrittingAsync = new object[] {null, this, null, null};
        }

        private static void MethodsForType(Type type, out SerialMethods packerMethods, out SerialMethods methods)
        {
            packerMethods = PrimitiveMethod.TryGetMethods(type);
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
            MethodsForType(typeof(T), out SerialMethods packerMethods, out SerialMethods methods);
            if (packerMethods != null)
            {
                _paramsWrittingPacker[0] = value;
                packerMethods.Write.Invoke(Packer, _paramsWrittingPacker);
            }
            else
            {
                var cache = new Dictionary<Object, int>();
                _paramsWritting[0] = value;
                _paramsWritting[2] = cache;
                methods.Write.Invoke(null, _paramsWritting);
            }
        }

        /// <summary>
        /// Serialize an object asynchronously into a stream
        /// </summary>
        /// <typeparam name="T">the type of object that will be serialized</typeparam>
        /// <param name="value">the object that will be serialized</param>
        /// <param name="cancellation">token to cancel the pending IO</param>
        public Task SerializeAsync<T>(T value, CancellationToken cancellation)
        {
            MethodsForType(typeof(T), out SerialMethods packerMethods, out SerialMethods methods);            
            
            _paramsWrittingPackerAsync[1] = cancellation;
            if (packerMethods != null)
            {
                _paramsWrittingPackerAsync[0] = value;
                return (Task)packerMethods.WriteAsync.Invoke(Packer, _paramsWrittingPackerAsync);
            }
            else
            {
                var cache = new Dictionary<Object, int>();
                
                _paramsWrittingAsync[2] = cache;
                _paramsWrittingAsync[3] = cancellation;

                _paramsWrittingAsync[0] = value;
                return (Task)methods.WriteAsync.Invoke(null, _paramsWrittingAsync);
            }
        }

        public Task SerializeAsync<T>(T value)
        {
            return SerializeAsync<T>(value, CancellationToken.None);
        }

        internal void WriteEnumerable<T>(IEnumerable<T> value, Dictionary<Object, int> cache)
        {
            if (value == null)
            {
                Packer.WriteInt32(0);
                return;
            }
            if (cache.TryGetValue(value, out int id))
            {
                Packer.WriteInt32(id);
                return;
            }
            id = cache.Count + 1;
            cache[value] = id;
            Packer.WriteInt32(id);

            MethodsForType(typeof(T), out SerialMethods packerMethods, out SerialMethods methods);
            Packer.WriteInt32(value.Count());
            if (packerMethods != null)
            {
                foreach (var item in value)
                {
                    _paramsWrittingPacker[0] = item;
                    packerMethods.Write.Invoke(Packer, _paramsWrittingPacker);
                }
            }
            else
            {
                foreach (var item in value)
                {
                    _paramsWritting[0] = item;
                    methods.Write.Invoke(null, _paramsWritting);
                }
            }
        }

        internal async Task WriteEnumerableAsync<T>(
            IEnumerable<T> value, 
            Dictionary<Object, int> cache,
            CancellationToken cancellation)
        {
            if (value == null)
            {
                await Packer.WriteInt32Async(0, cancellation);
                return;
            }
            if (cache.TryGetValue(value, out int id))
            {
                await Packer.WriteInt32Async(id, cancellation);
                return;
            }
            
            id = cache.Count + 1;
            cache[value] = id;
            await Packer.WriteInt32Async(id, cancellation);

            MethodsForType(typeof(T), out SerialMethods packerMethods, out SerialMethods methods);
            await Packer.WriteInt32Async(value.Count(), cancellation);
            if (packerMethods != null)
            {
                foreach (var item in value)
                {
                    _paramsWrittingPackerAsync[0] = item;                    
                    await (Task)packerMethods.WriteAsync.Invoke(Packer, _paramsWrittingPackerAsync);
                }
            }
            else
            {
                foreach (var item in value)
                {
                    _paramsWritting[0] = item;
                    await (Task)methods.WriteAsync.Invoke(null, _paramsWrittingAsync);
                }
            }
        }

        internal void WriteString(string value, Dictionary<Object, int> cache)
        {
            if (value == null)
            {
                Packer.WriteInt32(0);
                return;
            }
            if (cache.TryGetValue(value, out int id))
            {
                Packer.WriteInt32(id);
                return;
            }
            id = cache.Count + 1;
            cache[value] = id;
            Packer.WriteInt32(id);
            Packer.WriteString(value);
        }

        internal async Task WriteStringAsync(
            string value, 
            Dictionary<Object, int> cache,
            CancellationToken cancellation)
        {
            if (value == null)
            {
                await Packer.WriteInt32Async(0, cancellation);
                return;
            }
            if (cache.TryGetValue(value, out int id))
            {
                await Packer.WriteInt32Async(id, cancellation);
                return;
            }
            
            id = cache.Count + 1;
            cache[value] = id;
            await Packer.WriteInt32Async(id, cancellation);
            await Packer.WriteStringAsync(value, cancellation);
        }

        internal string ReadString(Dictionary<int, Object> cache)
        {
            var id = Packer.ReadInt32();
            if (id == 0) return null;
            if (cache.TryGetValue(id, out object value))
            {
                return (string)value;
            }

            var str = Packer.ReadString();
            cache[id] = str;
            return str;
        }

        internal async Task<string> ReadStringAsync(
            Dictionary<int, Object> cache,
            CancellationToken cancellation)
        {
            var id = await Packer.ReadInt32Async(cancellation);
            if (id == 0) return null;
            if (cache.TryGetValue(id, out object value))
            {
                return (string)value;
            }

            var str = await Packer.ReadStringAsync(cancellation);
            cache[id] = str;
            return str;
        }

        internal void WriteDict<K,V>(Dictionary<K,V> value, Dictionary<Object, int> cache)
        {
            if (value == null)
            {
                Packer.WriteInt32(0);
                return;
            }
            if (cache.TryGetValue(value, out int id))
            {
                Packer.WriteInt32(id);
                return;
            }
            id = cache.Count + 1;
            cache[value] = id;
            Packer.WriteInt32(id);

            MethodsForType(typeof(K), out SerialMethods packerMethodsK, out SerialMethods methodsK);
            MethodsForType(typeof(V), out SerialMethods packerMethodsV, out SerialMethods methodsV);

            Packer.WriteInt32(value.Count());
            if (packerMethodsK != null && packerMethodsV != null)
            {
                foreach (var item in value)
                {
                    _paramsWrittingPacker[0] = item.Key;
                    packerMethodsK.Write.Invoke(Packer, _paramsWrittingPacker);
                    _paramsWrittingPacker[0] = item.Value;
                    packerMethodsV.Write.Invoke(Packer, _paramsWrittingPacker);
                }
            }
            else if (packerMethodsK != null && methodsV != null)
            {
                foreach (var item in value)
                {
                    _paramsWrittingPacker[0] = item.Key;
                    packerMethodsK.Write.Invoke(Packer, new object[] { item.Key });
                    _paramsWritting[0] = item.Value;
                    methodsV.Write.Invoke(null, _paramsWritting);
                }
            }
            else if (methodsK != null && methodsV != null)
            {
                foreach (var item in value)
                {
                    _paramsWritting[0] = item.Key;
                    methodsK.Write.Invoke(null, _paramsWritting);
                    _paramsWritting[0] = item.Value;
                    methodsV.Write.Invoke(null, _paramsWritting);
                }
            }
            else
            {
                foreach (var item in value)
                {
                    _paramsWritting[0] = item.Key;
                    methodsK.Write.Invoke(null, _paramsWritting);
                    _paramsWritting[0] = item.Value;
                    packerMethodsV.Write.Invoke(Packer, _paramsWritting);
                }
            }
        }

        internal async Task WriteDictAsync<K,V>(
            Dictionary<K,V> value, 
            Dictionary<Object, int> cache,
            CancellationToken cancellation)
        {
            if (value == null)
            {
                await Packer.WriteInt32Async(0, cancellation);
                return;
            }
            if (cache.TryGetValue(value, out int id))
            {
                await Packer.WriteInt32Async(id, cancellation);
                return;
            }
            
            id = cache.Count + 1;
            cache[value] = id;
            await Packer.WriteInt32Async(id, cancellation);

            MethodsForType(typeof(K), out SerialMethods packerMethodsK, out SerialMethods methodsK);
            MethodsForType(typeof(V), out SerialMethods packerMethodsV, out SerialMethods methodsV);

            await Packer.WriteInt32Async(value.Count(), cancellation);
                        if (packerMethodsK != null && packerMethodsV != null)
            {
                foreach (var item in value)
                {
                    _paramsWrittingPackerAsync[0] = item.Key;
                    await (Task)packerMethodsK.WriteAsync.Invoke(Packer, _paramsWrittingPackerAsync);
                    _paramsWrittingPackerAsync[0] = item.Value;
                    await (Task)packerMethodsV.WriteAsync.Invoke(Packer, _paramsWrittingPackerAsync);
                }
            }
            else if (packerMethodsK != null && methodsV != null)
            {
                foreach (var item in value)
                {
                    _paramsWrittingPackerAsync[0] = item.Key;
                    await (Task)packerMethodsK.WriteAsync.Invoke(Packer, _paramsWrittingPackerAsync);
                    _paramsWrittingAsync[0] = item.Value;
                    await (Task)methodsV.WriteAsync.Invoke(null, _paramsWrittingAsync);
                }
            }
            else if (methodsK != null && methodsV != null)
            {
                foreach (var item in value)
                {
                    _paramsWrittingAsync[0] = item.Key;
                    await (Task)methodsK.WriteAsync.Invoke(null, _paramsWrittingAsync);
                    _paramsWrittingAsync[0] = item.Value;
                    await (Task)methodsV.WriteAsync.Invoke(null, _paramsWrittingAsync);
                }
            }
            else
            {
                foreach (var item in value)
                {
                    _paramsWrittingAsync[0] = item.Key;
                    await (Task)methodsK.WriteAsync.Invoke(null, _paramsWrittingAsync);
                    _paramsWrittingPackerAsync[0] = item.Value;
                    await (Task)packerMethodsV.WriteAsync.Invoke(Packer, _paramsWrittingPackerAsync);
                }
            }
        }

        internal Dictionary<K, V> ReadDict<K, V>(Dictionary<int, Object> cache)
        {
            var id = Packer.ReadInt32();
            if (id == 0) return null;
            if (cache.TryGetValue(id, out object value))
            {
                return (Dictionary<K, V>)value;
            }

            MethodsForType(typeof(K), out SerialMethods packerMethodsK, out SerialMethods methodsK);
            MethodsForType(typeof(V), out SerialMethods packerMethodsV, out SerialMethods methodsV);

            var count = Packer.ReadInt32();
            var dict = new Dictionary<K, V>(capacity: count);
            cache[id] = dict;
            if (packerMethodsK != null && packerMethodsV != null)
            {
                for (var i = 0; i < count; ++i)
                {
                    var k = (K)packerMethodsK.Read.Invoke(Packer, _paramsReadingPacker);
                    var v = (V)packerMethodsV.Read.Invoke(Packer, _paramsReadingPacker);
                    dict[k] = v;
                }
            }
            else if (packerMethodsK != null && methodsV != null)
            {
                for (var i = 0; i < count; ++i)
                {
                    var k = (K)packerMethodsK.Read.Invoke(Packer, _paramsReadingPacker);
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
                for (var i = 0; i < count; ++i)
                {
                    var k = (K)methodsK.Read.Invoke(null, _paramsReading);
                    var v = (V)packerMethodsV.Read.Invoke(Packer, _paramsReadingPacker);
                    dict[k] = v;
                }
            }
            return dict;
        }

        internal async Task<Dictionary<K, V>> ReadDictAsync<K, V>(
            Dictionary<int, Object> cache,
            CancellationToken cancellation)
        {
            var id = await Packer.ReadInt32Async(cancellation);
            if (id == 0) return null;
            if (cache.TryGetValue(id, out object value))
            {
                return (Dictionary<K, V>)value;
            }

            MethodsForType(typeof(K), out SerialMethods packerMethodsK, out SerialMethods methodsK);
            MethodsForType(typeof(V), out SerialMethods packerMethodsV, out SerialMethods methodsV);

            var count = await Packer.ReadInt32Async(cancellation);
            var dict = new Dictionary<K, V>(capacity: count);
            cache[id] = dict;

            if (packerMethodsK != null && packerMethodsV != null)
            {
                for (var i = 0; i < count; ++i)
                {
                    var k = await (Task<K>)packerMethodsK.ReadAsync.Invoke(Packer, _paramsReadingPackerAsync);
                    var v = await (Task<V>)packerMethodsV.ReadAsync.Invoke(Packer, _paramsReadingPackerAsync);
                    dict[k] = v;
                }
            }
            else if (packerMethodsK != null && methodsV != null)
            {
                for (var i = 0; i < count; ++i)
                {
                    var k = await (Task<K>)packerMethodsK.ReadAsync.Invoke(Packer, _paramsReadingPackerAsync);
                    var v = await (Task<V>)methodsV.ReadAsync.Invoke(null, _paramsReadingAsync);
                    dict[k] = v;
                }
            }
            else if (methodsK != null && methodsV != null)
            {
                for (var i = 0; i < count; ++i)
                {
                    var k = await (Task<K>)methodsK.ReadAsync.Invoke(null, _paramsReadingAsync);
                    var v = await (Task<V>)methodsV.ReadAsync.Invoke(null, _paramsReadingAsync);
                    dict[k] = v;
                }
            }
            else
            {
                for (var i = 0; i < count; ++i)
                {
                    var k = await (Task<K>)methodsK.ReadAsync.Invoke(null, _paramsReadingAsync);
                    var v = await (Task<V>)packerMethodsV.ReadAsync.Invoke(Packer, _paramsReadingPackerAsync);
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
        /// <exception cref="System.IO.EndOfStreamException">The input stream end reached</exception>
        public T Deserialize<T>()
        {
            MethodsForType(typeof(T), out SerialMethods packerMethods, out SerialMethods methods);
            if (packerMethods != null)
            {
                return (T)packerMethods.Read.Invoke(Packer, _paramsReadingPacker);
            }
            else
            {
                var cache = new Dictionary<int, Object>();
                _paramsReading[1] = cache;
                return (T)methods.Read.Invoke(null, _paramsReading);
            }
        }

        /// <summary>
        /// deserialize an object asynchronously as type T from a stream. 
        /// </summary>
        /// <typeparam name="T">The object type that will be deserialize</typeparam>
        /// <param name="cancellation">token to cancel the pending IO</param>
        /// <returns>The deserialized object</returns>
        /// <exception cref="System.IO.EndOfStreamException">The input stream end reached</exception>
        public Task<T> DeserializeAsync<T>(CancellationToken cancellation)
        {
            MethodsForType(typeof(T), out SerialMethods packerMethods, out SerialMethods methods);
            _paramsReadingPackerAsync[0] = cancellation;
            if (packerMethods != null)
            {                
                return (Task<T>)packerMethods.ReadAsync.Invoke(Packer, _paramsReadingPackerAsync);
            }
            else
            {
                var cache = new Dictionary<int, Object>();
                _paramsReadingAsync[1] = cache;
                _paramsReadingAsync[2] = cancellation;
                return (Task<T>)methods.ReadAsync.Invoke(null, _paramsReadingAsync);
            }
        }

        public Task<T> DeserializeAsync<T>()
        {
            return DeserializeAsync<T>(CancellationToken.None);
        }

        internal List<T> ReadList<T>(Dictionary<int, Object> cache)
        {
            var id = Packer.ReadInt32();
            if (id == 0) return null;
            if (cache.TryGetValue(id, out object value))
            {
                return (List<T>)value;
            }

            MethodsForType(typeof(T), out SerialMethods packerMethods, out SerialMethods methods);
            var count = Packer.ReadInt32();
            var list = new List<T>(capacity: count);
            cache[id] = list;

            if (packerMethods != null)
            {
                for (var i = 0; i < count; ++i)
                {
                    list.Add((T)packerMethods.Read.Invoke(Packer, _paramsReadingPacker));
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

        internal async Task<List<T>> ReadListAsync<T>(
            Dictionary<int, Object> cache,
            CancellationToken cancellation)
        {
            var id = await Packer.ReadInt32Async(cancellation);
            if (id == 0) return null;
            if (cache.TryGetValue(id, out object value))
            {
                return (List<T>)value;
            }

            MethodsForType(typeof(T), out SerialMethods packerMethods, out SerialMethods methods);
            var count = await Packer.ReadInt32Async(cancellation);
            var list = new List<T>(capacity: count);
            cache[id] = list;

            if (packerMethods != null)
            {
                for (var i = 0; i < count; ++i)
                {
                    list.Add(await (Task<T>)packerMethods.ReadAsync.Invoke(Packer, _paramsReadingPackerAsync));
                }
            }
            else
            {
                for (var i = 0; i < count; ++i)
                {
                    list.Add(await (Task<T>)methods.ReadAsync.Invoke(null, _paramsReadingAsync));
                }
            }
            return list;
        }

        internal T[] ReadArray<T>(Dictionary<int, Object> cache)
        {
            var id = Packer.ReadInt32();
            if (id == 0) return null;
            if (cache.TryGetValue(id, out object value))
            {
                return (T[])value;
            }

            MethodsForType(typeof(T), out SerialMethods packerMethods, out SerialMethods methods);
            var count = Packer.ReadInt32();
            var list = new T[count];
            cache[id] = list;

            if (packerMethods != null)
            {
                for (var i = 0; i < count; ++i)
                {
                    list[i] = (T)packerMethods.Read.Invoke(Packer, _paramsReadingPacker);
                }
            }
            else
            {
                for (var i = 0; i < count; ++i)
                {
                    list[i] = (T)methods.Read.Invoke(null, _paramsReading);
                }
            }
            return list;
        }

        internal async Task<T[]> ReadArrayAsync<T>(
            Dictionary<int, Object> cache,
            CancellationToken cancellation)
        {
            var id = await Packer.ReadInt32Async(cancellation);
            if (id == 0) return null;
            if (cache.TryGetValue(id, out object value))
            {
                return (T[])value;
            }

            MethodsForType(typeof(T), out SerialMethods packerMethods, out SerialMethods methods);
            var count = await Packer.ReadInt32Async(cancellation);
            var list = new T[count];
            cache[id] = list;

            if (packerMethods != null)
            {
                for (var i = 0; i < count; ++i)
                {
                    list[i] = await (Task<T>)packerMethods.ReadAsync.Invoke(Packer, _paramsReadingPackerAsync);
                }
            }
            else
            {
                for (var i = 0; i < count; ++i)
                {
                    list[i] = await (Task<T>)methods.ReadAsync.Invoke(null, _paramsReadingAsync);
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
        private static void CacheSerializable(Type serializableType)
        {
            if (TypeMethodsMap.ContainsKey(serializableType)) return;
            if (!serializableType.IsSerializableClass())
            {
                var msg = $"{serializableType.Name} must be a primitive type or class with GSerializable attribute, \n";
                msg += " and must NOT be a generic type, and must be a non-abstract and public type";
                throw new NotSupportedException(msg);
            }
            PrepareForAssembly(serializableType.Assembly);
        }

        /// <summary>
        /// Cache all serializable classes in an assembly.
        /// </summary>
        /// <param name="assembly">The assembly name</param>
        public static void PrepareForAssembly(Assembly assembly)
        {
            var generatedAssemblyName = $"gserialize2.gen{assembly.GetHashCode()}.dll";
            AddAssembly(assembly, generatedAssemblyName);
        }
    }
}
