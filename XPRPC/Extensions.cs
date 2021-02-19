/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace XPRPC
{
    static class TypeExtension
    {
        internal static string GeneratedDispatcherClassName(this Type serviceType)
        {
            return $"Generated{serviceType.Name}_dispatcher";
        }

        internal static string GeneratedProxyClassName(this Type serviceType)
        {
            return $"Generated{serviceType.Name}_proxy";
        }

        internal static List<MethodInfo> DeclaredMethods(this Type serviceType)
        {            
            var methods = GetMethods(serviceType, new HashSet<Type>());
            //过滤掉event方法（IsSpecialName == true)
            var result = new List<MethodInfo>(
                from m in methods
                where m.IsIndexer() || !m.IsSpecialName
                select m);
            result.Sort((x, y) => string.CompareOrdinal(x.Name, y.Name));
            return result;
        }

        private static List<MethodInfo> GetMethods(Type serviceType, HashSet<Type> exclusions)
        {
            exclusions.Add(serviceType);
            var methods = new List<MethodInfo>(serviceType.GetMethods());
            var info = serviceType.GetTypeInfo();
            foreach(var baseInterface in info.ImplementedInterfaces)
            {
                if (baseInterface != typeof(IDisposable) && !exclusions.Contains(baseInterface))
                {
                    methods.AddRange(GetMethods(baseInterface, exclusions));
                }
            }
            return methods;
        }

        internal static List<EventInfo> DeclaredEvents(this Type serviceType)
        {
            return GetEvents(serviceType, new HashSet<Type>());
        }

        private static List<EventInfo> GetEvents(Type serviceType, HashSet<Type> exclusions)
        {
            exclusions.Add(serviceType);
            var events = new List<EventInfo>(serviceType.GetEvents());
            var info = serviceType.GetTypeInfo();
            foreach(var baseInterface in info.ImplementedInterfaces)
            {
                if (!exclusions.Contains(baseInterface))
                {
                    events.AddRange(GetEvents(baseInterface, exclusions));
                }
            }
            return events;
        }

        internal static bool IsVoid(this Type serviceType)
        {
            return serviceType == typeof(void);
        }

        internal static bool IsTask(this Type serviceType)
        {
            return serviceType == typeof(Task);
        }

        internal static bool IsGenericTask(this Type serviceType)
        {
            return serviceType.FullName.StartsWith("System.Threading.Tasks.Task`1");
        }

        internal static string CompilableClassName(this Type type)
        {
            if (type.FullName.StartsWith("System.Collections.Generic.List`1"))
            {                
                var elementType = type.GetGenericArguments()[0];
                return $"List<{elementType.CompilableClassName()}>";
            }
            if (type.FullName.StartsWith("System.Collections.Generic.Dictionary`2"))
            {
                var kType = type.GetGenericArguments()[0];
                var vType = type.GetGenericArguments()[1];
                return $"Dictionary<{kType.CompilableClassName()}, {vType.CompilableClassName()}>";
            }
            if (type.FullName.StartsWith("System.Threading.Tasks.Task`1"))
            {
                var valueType = type.GetGenericArguments()[0];
                return $"Task<{valueType.CompilableClassName()}>";
            }

            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                return $"{elementType.CompilableClassName()}[]";
            }
            return type.FullName.Replace('+', '.');
        }

        internal static string VersionName(this Type type)
        {
            var builder = new StringBuilder(type.FullName);
            foreach(var m in type.GetMethods())
            {
                builder.Append(m.ReturnType.Name);
                builder.Append(m.Name);
                foreach(var p in m.GetParameters())
                {
                    builder.Append(p.ParameterType.Name);
                    builder.Append(p.Name);
                }
            }
            var hash = FNVHash1(builder.ToString());
            return $"{type.FullName}{hash}";
        }

        private static int FNVHash1(String data)
        {
            long p = 16777619;
            long hash = 2166136261L;
            for (int i = 0; i < data.Length; ++i) hash = (hash ^ data[i]) * p;
            hash += hash << 13;
            hash ^= hash >> 7;
            hash += hash << 3;
            hash ^= hash >> 17;
            hash += hash << 5;
            return (int)hash;
        }
    }

    static class EventInfoExtension
    {
        public static string RegisteredFieldName(this EventInfo info)
        {
            return $"_registered_{info.Name}";
        }

        public static string AddingMethodName(this EventInfo info)
        {
            return $"AddE_{info.Name}";
        }

        public static string RemovingMethodName(this EventInfo info)
        {
            return $"RemoveE_{info.Name}";
        }

        public static string HandlingMethodName(this EventInfo info)
        {
            return $"OnE_{info.Name}";
        }

        public static string ArgsTypeName(this EventInfo info)
        {
            var t = info.EventHandlerType.GenericTypeArguments[0];
            return t.CompilableClassName();
        }
    }

    static class MethodInfoExtension
    {
        public static bool IsSynchronized(this MethodInfo method)
        {
            return !method.ReturnType.IsGenericTask() && !method.ReturnType.IsTask();
        }

        public static string ReturnName(this MethodInfo method)
        {
            if (method.ReturnType.IsVoid()) return "void";
            return method.ReturnType.CompilableClassName();
        }

        public static bool IsIndexer(this MethodInfo method)
        {
            return method.IsGetter() || method.IsGetter();
        }

        public static bool IsGetter(this MethodInfo method)
        {
            return method.IsSpecialName && method.Name == "get_Item";
        }

        public static bool IsSetter(this MethodInfo method)
        {
            return method.IsSpecialName && method.Name == "set_Item";
        }
    }
}