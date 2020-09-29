/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Reflection;

namespace GSerialize
{
    static class TypeExtension
    {
        internal static string GeneratedClassName(this Type type)
        {
            return $"Serial_{type.FullName.Replace('.', '_').Replace('+', '_')}";
        }

        internal static string GeneratedFullClassName(this Type type)
        {
            return $"GSerialize.Generated.{type.GeneratedClassName()}";
        }

        internal static string GeneratedClassName2(this Type type)
        {
            return $"Serial2_{type.FullName.Replace('.', '_').Replace('+', '_')}";
        }

        internal static string GeneratedFullClassName2(this Type type)
        {
            return $"GSerialize.Generated.{type.GeneratedClassName2()}";
        }

        internal static string VisibleClassName(this Type type)
        {
            if (type.Name == "List`1")
            {
                var elementType = type.GetGenericArguments()[0];
                return $"List<{elementType.VisibleClassName()}>";
            }
            if (type.Name == "Dictionary`2")
            {
                var kType = type.GetGenericArguments()[0];
                var vType = type.GetGenericArguments()[1];
                return $"Dictionary<{kType.VisibleClassName()}, {vType.VisibleClassName()}>";
            }
            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                return $"{elementType.VisibleClassName()}[]";
            }
            return type.FullName.Replace('+', '.');
        }

        internal static bool IsSerializableClass(this Type type)
        {
            if (type.ContainsGenericParameters) return false;
            if (type.IsAbstract) return false;
            if (!type.IsPublic) return false;

            if (type.Namespace != null)
            {
                if (type.Namespace.StartsWith("System") ||
                    type.Namespace.StartsWith("Microsoft"))
                {
                    return false;
                }
            }
            else
            {
                return false;
            }            
            
            var defaultConstructor = type.GetConstructor(Array.Empty<Type>());
            if (defaultConstructor == null) return false;

            return type.IsDefined(typeof(SerializableAttribute), false);
        }
    }

    static class MemberInfoExtension
    {
        internal static bool IsOptional(this PropertyInfo info)
        {
            if (info.PropertyType.Name == "Nullable`1") return true;
            if (info.PropertyType.IsValueType) return false;
            
            return !info.IsDefined(typeof(NonNullAttribute));
        }

        internal static bool IsOptional(this FieldInfo info)
        {
            if (info.FieldType.Name == "Nullable`1") return true;
            if (info.FieldType.IsValueType) return false;
            
            return !info.IsDefined(typeof(NonNullAttribute));
        }

        internal static bool IsIgnored(this MemberInfo info)
        {
            return info.IsDefined(typeof(NonSerializedAttribute), inherit: false);
        }
    }
}