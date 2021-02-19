/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
using System;

namespace XPRPC
{
    class SerializeStatement
    {
        public static string SerializeObject(Type type, string objectName)
        {
            if (type.FullName.StartsWith("System.Collections.Generic.List"))
                return $"CollectionPacker.WriteList({objectName}, serializer);";

            if (type.FullName.StartsWith("System.Nullable"))
                return $"CollectionPacker.WriteNullable({objectName}, serializer);";

            if (type.IsArray)
            {
                if(type.GetArrayRank() == 1 && type.HasElementType)
                    return $"CollectionPacker.WriteArray({objectName}, serializer);";
                else
                    throw new NotSupportedException($"{type.FullName} is not a supported type");
            }

            if (type.FullName.StartsWith("System.Collections.Generic.Dictionary"))
                return $"CollectionPacker.WriteDict({objectName}, serializer);";

            if (type.IsEnum)
                return $"CollectionPacker.WriteEnum({objectName}, serializer);";
            
            return $"serializer.Serialize({objectName});";
        }

        public static string DeserializeObject(Type type)
        {
            if (type.FullName.StartsWith("System.Collections.Generic.List"))
            {
                var elementType = type.GetGenericArguments()[0].CompilableClassName();
                return $"CollectionPacker.ReadList<{elementType}>(serializer);";
            }

            if (type.FullName.StartsWith("System.Nullable"))
            {
                var valueType = type.GetGenericArguments()[0];
                return $"CollectionPacker.ReadNullable<{valueType.CompilableClassName()}>(serializer);";
            }

            if (type.IsArray)
            {
                if(type.GetArrayRank() == 1 && type.HasElementType)
                    return $"CollectionPacker.ReadArray<{type.GetElementType().CompilableClassName()}>(serializer);";
                else
                    throw new NotSupportedException($"{type.FullName} is not a supported type");
            }

            if (type.FullName.StartsWith("System.Collections.Generic.Dictionary"))
            {
                var keyType = type.GetGenericArguments()[0].CompilableClassName();
                var valueType = type.GetGenericArguments()[1].CompilableClassName();
                return $"CollectionPacker.ReadDict<{keyType}, {valueType}>(serializer);";
            }

            if (type.IsEnum)
                return $"CollectionPacker.ReadEnum<{type.CompilableClassName()}>(serializer);";
            
            return $"serializer.Deserialize<{type.CompilableClassName()}>();";
        }
    }
}