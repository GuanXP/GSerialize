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

namespace GSerialize
{
    internal sealed class PropertyFieldInfo
    {
        internal Type MemberType;
        internal string MemberName;
        internal bool IsOptional;

        internal static List<PropertyFieldInfo> FindProperties(Type type)
        {
            var result = new List<PropertyFieldInfo>();
            var properties = from p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                where p.CanWrite && p.CanRead && !p.IsIgnored()
                select p;
            foreach(var p in properties)
            {
                result.Add(new PropertyFieldInfo 
                {
                    MemberType = p.PropertyType, 
                    MemberName = p.Name,
                    IsOptional = p.IsOptional()
                });
            }

            var fields = from f in type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                where !f.IsInitOnly && !f.IsIgnored()
                select f;

            foreach (var f in fields)
            {
                result.Add(new PropertyFieldInfo 
                { 
                    MemberType = f.FieldType, 
                    MemberName = f.Name,
                    IsOptional = f.IsOptional()
                });
            }
            result.Sort((x,y)=>string.Compare(x.MemberName, y.MemberName));
            return result;
        } 
        
        internal string GenericParams()
        {
            var sb = new StringBuilder("<");
            var first = true;
            foreach(var p in MemberType.GetGenericArguments())
            {
                if (first)
                {
                    first = false;
                    sb.Append(p.VisibleClassName());
                }
                else
                {
                    sb.Append(",");
                    sb.Append(p.VisibleClassName());
                }
            }
            if (first && MemberType.IsArray && MemberType.HasElementType)
            {
                sb.Append(MemberType.GetElementType().VisibleClassName());
            }
            sb.Append(">");
            return sb.ToString();
        }
    }
}