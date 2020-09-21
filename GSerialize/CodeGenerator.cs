﻿/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
using System;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Runtime.Loader;

namespace GSerialize
{
    sealed class CodeGenerator
    {
        static void LoadReferencedAssemblies(Assembly assembly, 
            List<MetadataReference> references, List<Assembly> loadedAssemblies)
        {
            if (loadedAssemblies.FirstOrDefault(x => x == assembly) != null) return;

            loadedAssemblies.Add(assembly);
            references.Add(MetadataReference.CreateFromFile(assembly.Location));
            foreach (var refName in assembly.GetReferencedAssemblies())
            {
                var refAssembly = Assembly.Load(refName);
                LoadReferencedAssemblies(refAssembly, references, loadedAssemblies);
            }
        }

        public static Assembly CompileSerialable(
            List<Type> types, 
            List<Assembly> referencedAssemblies,
            string generatedAssemblyName)
        {
            var code = GenerateCode(types);
            var tree = SyntaxFactory.ParseSyntaxTree(code);

            var references = new List<MetadataReference>();
            foreach(var a in referencedAssemblies)
            {
                references.Add(MetadataReference.CreateFromFile(a.Location));
            }

            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var compilation = CSharpCompilation.Create(generatedAssemblyName)
                .WithOptions(options)
                .AddReferences(references)
                .AddSyntaxTrees(tree);
            var mem = new MemoryStream();
            var result = compilation.Emit(mem);
            if (!result.Success)
            {
                throw new NotSupportedException(result.Diagnostics[0].ToString());
            }
            mem.Seek(0, SeekOrigin.Begin);
            return AssemblyLoadContext.Default.LoadFromStream(mem);
        }

        private static string GenerateCode(List<Type> types)
        {
            var code = new List<String>();
            code.Add("using System.IO;");
            code.Add("using GSerialize;");
            code.Add("using System.Threading.Tasks;");
            code.Add("namespace GSerialize.Generated {");            

            foreach(var t in types)
            {
                var classCode = GenerateCodeForType(t);
                code.AddRange(classCode);
            }

            code.Add("}");

            return CodeLinesToString(code);
        }

        private static string CodeLinesToString(List<string> codeLines)
        {
            var builder = new StringBuilder();
            foreach (var s in codeLines)
            {
                builder.Append(s);
            }
            return builder.ToString();
        }

        private static List<string> GenerateCodeForType(Type type)
        {
            var code = new List<String>();
            code.Add($"public class {type.GeneratedClassName()}");
            code.Add("{");
            code.AddRange(GenerateWriteMethod(type));
            code.AddRange(GenerateReadMethod(type));
            code.Add("}");

            return code;
        }        

        static readonly List<StatementGenerator> s_statementGenerators = new List<StatementGenerator>
        {
            new PrimitiveStatementGenerator(typeof(UInt16), "UInt16"),
            new PrimitiveStatementGenerator(typeof(Int16), "Int16"),
            new PrimitiveStatementGenerator(typeof(UInt32), "UInt32"),
            new PrimitiveStatementGenerator(typeof(Int32), "Int32"),
            new PrimitiveStatementGenerator(typeof(UInt64), "UInt64"),
            new PrimitiveStatementGenerator(typeof(Int64), "Int64"),
            new PrimitiveStatementGenerator(typeof(Byte), "Byte"),
            new PrimitiveStatementGenerator(typeof(SByte), "SByte"),
            new PrimitiveStatementGenerator(typeof(Boolean), "Bool"),
            new PrimitiveStatementGenerator(typeof(Char), "Char"),
            new PrimitiveStatementGenerator(typeof(string), "String"),
            new PrimitiveStatementGenerator(typeof(double), "Double"),
            new PrimitiveStatementGenerator(typeof(float), "Float"),
            new PrimitiveStatementGenerator(typeof(decimal), "Decimal"),
            new PrimitiveStatementGenerator(typeof(DateTime), "DateTime"),
            new PrimitiveStatementGenerator(typeof(TimeSpan), "TimeSpan"),
            new PrimitiveStatementGenerator(typeof(Guid), "Guid"),
            new SerializableStatementGenerator(),
            new ArrayStatementGenerator(),
            new ListStatementGenerator(),
            new DictStatementGenerator(),
            new EnumStatementGenerator(),
            new NullableStatementGenerator(),
        };

        private static List<string> GenerateWriteMethod(Type type)
        {
            var code = new List<string>();
            code.Add($"public static void Write({type.VisibleClassName()} value, Serializer serializer)");
            code.Add("{");
            code.Add("var packer = serializer.Packer;");
            foreach (var p in FindProperties(type))
            {                
                code.AddRange(GeneratePropertyWrite(p));
            }
            code.Add("}");

            code.Add($"public static async Task WriteAsync({type.VisibleClassName()} value, Serializer serializer)");
            code.Add("{");
            code.Add("var packer = serializer.Packer;");
            foreach (var p in FindProperties(type))
            {                
                code.AddRange(GeneratePropertyAsyncWrite(p));
            }
            code.Add("}");
            return code;
        }

        private static List<string> WrittingCode(PropertyFieldInfo p, string statementWrite)
        {
            var code = new List<string>();
            if (p.IsOptional)
            {
                code.Add($"if (value.{p.MemberName} == null) packer.WriteBool(false);");
                code.Add("else { packer.WriteBool(true);");
                code.Add(statementWrite);
                code.Add("}");
            }
            else
            {
                code.Add(statementWrite);
            }
            return code;
        }

        private static List<string> WrittingAsyncCode(PropertyFieldInfo p, string statementWrite)
        {
            var code = new List<string>();
            if (p.IsOptional)
            {
                code.Add($"if (value.{p.MemberName} == null) await packer.WriteBoolAsync(false);");
                code.Add("else { await packer.WriteBoolAsync(true);");
                code.Add($"await {statementWrite}");
                code.Add("}");
            }
            else
            {
                code.Add($"await {statementWrite}");
            }
            return code;
        }

        private static List<string> GeneratePropertyWrite(PropertyFieldInfo p)
        {
            foreach(var gen in s_statementGenerators)
            {
                if (gen.Matches(p))
                {
                    return WrittingCode(p, gen.WrittingStatement(p));
                }
            }
            throw new NotSupportedException($"{p.MemberType} of {p.MemberName} is not a supported type");
        }

        private static List<string> GeneratePropertyAsyncWrite(PropertyFieldInfo p)
        {
            foreach(var gen in s_statementGenerators)
            {
                if (gen.Matches(p))
                {
                    return WrittingAsyncCode(p, gen.WrittingAsyncStatement(p));
                }
            }
            throw new NotSupportedException($"{p.MemberType} of {p.MemberName} is not a supported type");
        }

        private static List<string> GenerateReadMethod(Type type)
        {
            var code = new List<string>();
            var ReturnClassName = type.VisibleClassName();
            code.Add($"public static {ReturnClassName} Read(Serializer serializer)");
            code.Add("{");
            code.Add("var packer = serializer.Packer;");           
            code.Add($"return new {ReturnClassName}");
            code.Add("{");
            foreach (var p in FindProperties(type))
            {                
                code.Add(GeneratePropertyRead(p));
            }
            code.Add("};");
            code.Add("}");

            code.Add($"public static async Task<{ReturnClassName}> ReadAsync(Serializer serializer)");
            code.Add("{");
            code.Add("var packer = serializer.Packer;");           
            code.Add($"return new {ReturnClassName}");
            code.Add("{");
            foreach (var p in FindProperties(type))
            {                
                code.Add(GeneratePropertyAsyncRead(p));
            }
            code.Add("};");
            code.Add("}");
            return code;
        }

        private static string ReadingCode(PropertyFieldInfo p, string statementRead)
        {
            if (p.IsOptional)
            {
                return $"{p.MemberName} = (packer.ReadBool() ? {statementRead} : null),";
            }
            else
            {
               return $"{p.MemberName} = {statementRead},";
            }
        }

        private static string ReadingAsyncCode(PropertyFieldInfo p, string statementRead)
        {
            if (p.IsOptional)
            {
                return $"{p.MemberName} = ((await packer.ReadBoolAsync()) ? await {statementRead} : null),";
            }
            else
            {
               return $"{p.MemberName} = await {statementRead},";
            }
        }

        private static string GeneratePropertyRead(PropertyFieldInfo p)
        {
            foreach(var gen in s_statementGenerators)
            {
                if (gen.Matches(p))
                {
                    return ReadingCode(p, gen.ReadingStatement(p));
                }
            }
            throw new NotSupportedException($"{p.MemberType} of {p.MemberName} is not a supported type");
        }

        private static string GeneratePropertyAsyncRead(PropertyFieldInfo p)
        {
            foreach(var gen in s_statementGenerators)
            {
                if (gen.Matches(p))
                {
                    return ReadingAsyncCode(p, gen.ReadingAsyncStatement(p));
                }
            }
            throw new NotSupportedException($"{p.MemberType} of {p.MemberName} is not a supported type");
        }

        private static List<PropertyFieldInfo> FindProperties(Type type)
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
    }

    static class MemberInfoExtension
    {
        internal static bool IsOptional(this PropertyInfo info)
        {
            return !info.PropertyType.IsValueType && info.IsDefined(typeof(OptionalAttribute), inherit: false);
        }

        internal static bool IsOptional(this FieldInfo info)
        {
            return !info.FieldType.IsValueType && info.IsDefined(typeof(OptionalAttribute), inherit: false);
        }

        internal static bool IsIgnored(this MemberInfo info)
        {
            return info.IsDefined(typeof(IgnoredAttribute), inherit: false);
        }
    }

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

        internal static bool IsGSerializable(this Type type)
        {
            return type.IsDefined(typeof(GSerializableAttribute), false);
        }
    }

    class PropertyFieldInfo
    {
        internal Type MemberType;
        internal string MemberName;
        internal bool IsOptional;
    }

    interface StatementGenerator
    {
        bool Matches(PropertyFieldInfo p);
        string ReadingStatement(PropertyFieldInfo p);
        string WrittingStatement(PropertyFieldInfo p);
        string ReadingAsyncStatement(PropertyFieldInfo p);
        string WrittingAsyncStatement(PropertyFieldInfo p);
    }

    class PrimitiveStatementGenerator : StatementGenerator
    {
        private readonly string _packerTypeName;
        private readonly Type _matchedType;
        internal PrimitiveStatementGenerator(Type matchedType, string packerTypeName)
        {
            _packerTypeName = packerTypeName;
            _matchedType = matchedType;
        }

        public bool Matches(PropertyFieldInfo p)
        {
            return p.MemberType == _matchedType;
        }

        public string ReadingStatement(PropertyFieldInfo p)
        {
            return $"packer.Read{_packerTypeName}()";
        }

        public string ReadingAsyncStatement(PropertyFieldInfo p)
        {
            return $"packer.Read{_packerTypeName}Async()";
        }

        public string WrittingStatement(PropertyFieldInfo p)
        {
            return $"packer.Write{_packerTypeName}(value.{p.MemberName});";
        }

        public string WrittingAsyncStatement(PropertyFieldInfo p)
        {
            return $"packer.Write{_packerTypeName}Async(value.{p.MemberName});";
        }
    }

    class SerializableStatementGenerator : StatementGenerator
    {
        public bool Matches(PropertyFieldInfo p)
        {
            return p.MemberType.IsDefined(typeof(GSerializableAttribute), inherit: false);
        }

        public string ReadingStatement(PropertyFieldInfo p)
        {
            return $"{p.MemberType.GeneratedClassName()}.Read(serializer)";
        }

        public string ReadingAsyncStatement(PropertyFieldInfo p)
        {
            return $"{p.MemberType.GeneratedClassName()}.ReadAsync(serializer)";
        }

        public string WrittingStatement(PropertyFieldInfo p)
        {
            return $"{p.MemberType.GeneratedClassName()}.Write(value.{p.MemberName}, serializer);";
        }

        public string WrittingAsyncStatement(PropertyFieldInfo p)
        {
            return $"{p.MemberType.GeneratedClassName()}.WriteAsync(value.{p.MemberName}, serializer);";
        }
    }

    class ListStatementGenerator : StatementGenerator
    {
        public bool Matches(PropertyFieldInfo p)
        {
            return p.MemberType.Name == "List`1" && p.MemberType.IsGenericType;
        }

        public string ReadingStatement(PropertyFieldInfo p)
        {
            var itemType = p.MemberType.GetGenericArguments()[0];
            return $"CollectionPacker.ReadList<{itemType.VisibleClassName()}>(serializer)";
        }

        public string ReadingAsyncStatement(PropertyFieldInfo p)
        {
            var itemType = p.MemberType.GetGenericArguments()[0];
            return $"CollectionPacker.ReadListAsync<{itemType.VisibleClassName()}>(serializer)";
        }

        public string WrittingStatement(PropertyFieldInfo p)
        {
            var itemType = p.MemberType.GetGenericArguments()[0];
            return $"CollectionPacker.WriteList<{itemType.VisibleClassName()}>(value.{p.MemberName}, serializer);";
        }

        public string WrittingAsyncStatement(PropertyFieldInfo p)
        {
            var itemType = p.MemberType.GetGenericArguments()[0];
            return $"CollectionPacker.WriteListAsync<{itemType.VisibleClassName()}>(value.{p.MemberName}, serializer);";
        }
    }

    class ArrayStatementGenerator : StatementGenerator
    {
        public bool Matches(PropertyFieldInfo p)
        {
            return p.MemberType.IsArray && p.MemberType.GetArrayRank() == 1 && p.MemberType.HasElementType;
        }

        public string ReadingStatement(PropertyFieldInfo p)
        {
            var elementType = p.MemberType.GetElementType();
            return $"CollectionPacker.ReadArray<{elementType.VisibleClassName()}>(serializer)";
        }

        public string ReadingAsyncStatement(PropertyFieldInfo p)
        {
            var elementType = p.MemberType.GetElementType();
            return $"CollectionPacker.ReadArrayAsync<{elementType.VisibleClassName()}>(serializer)";
        }

        public string WrittingStatement(PropertyFieldInfo p)
        {
            var elementType = p.MemberType.GetElementType();
            return $"CollectionPacker.WriteArray<{elementType.VisibleClassName()}>(value.{p.MemberName}, serializer);";
        }

        public string WrittingAsyncStatement(PropertyFieldInfo p)
        {
            var elementType = p.MemberType.GetElementType();
            return $"CollectionPacker.WriteArrayAsync<{elementType.VisibleClassName()}>(value.{p.MemberName}, serializer);";
        }
    }

    class DictStatementGenerator : StatementGenerator
    {
        public bool Matches(PropertyFieldInfo p)
        {
            return p.MemberType.Name == "Dictionary`2" && p.MemberType.IsGenericType;
        }

        public string ReadingStatement(PropertyFieldInfo p)
        {
            var keyType = p.MemberType.GetGenericArguments()[0];
            var valueType = p.MemberType.GetGenericArguments()[1];
            return $"CollectionPacker.ReadDict<{keyType.VisibleClassName()},{valueType.VisibleClassName()}>(serializer)";
        }

        public string ReadingAsyncStatement(PropertyFieldInfo p)
        {
            var keyType = p.MemberType.GetGenericArguments()[0];
            var valueType = p.MemberType.GetGenericArguments()[1];
            return $"CollectionPacker.ReadDictAsync<{keyType.VisibleClassName()},{valueType.VisibleClassName()}>(serializer)";
        }

        public string WrittingStatement(PropertyFieldInfo p)
        {
            var keyType = p.MemberType.GetGenericArguments()[0];
            var valueType = p.MemberType.GetGenericArguments()[1];
            return $"CollectionPacker.WriteDict<{keyType.VisibleClassName()},{valueType.VisibleClassName()}>(value.{p.MemberName}, serializer);";
        }

        public string WrittingAsyncStatement(PropertyFieldInfo p)
        {
            var keyType = p.MemberType.GetGenericArguments()[0];
            var valueType = p.MemberType.GetGenericArguments()[1];
            return $"CollectionPacker.WriteDictAsync<{keyType.VisibleClassName()},{valueType.VisibleClassName()}>(value.{p.MemberName}, serializer);";
        }
    }

    class EnumStatementGenerator : StatementGenerator
    {
        public bool Matches(PropertyFieldInfo p)
        {
            return p.MemberType.IsEnum;
        }

        public string ReadingStatement(PropertyFieldInfo p)
        {
            return $"CollectionPacker.ReadEnum<{p.MemberType.VisibleClassName()}>(serializer)";
        }

        public string ReadingAsyncStatement(PropertyFieldInfo p)
        {
            return $"CollectionPacker.ReadEnumAsync<{p.MemberType.VisibleClassName()}>(serializer)";
        }

        public string WrittingStatement(PropertyFieldInfo p)
        {
            return $"CollectionPacker.WriteEnum<{p.MemberType.VisibleClassName()}>(value.{p.MemberName}, serializer);";
        }

        public string WrittingAsyncStatement(PropertyFieldInfo p)
        {
            return $"CollectionPacker.WriteEnumAsync<{p.MemberType.VisibleClassName()}>(value.{p.MemberName}, serializer);";
        }
    }

    class NullableStatementGenerator : StatementGenerator
    {
        public bool Matches(PropertyFieldInfo p)
        {
            return p.MemberType.Name == "Nullable`1" && p.MemberType.IsGenericType;
        }

        public string ReadingStatement(PropertyFieldInfo p)
        {
            var valueType = p.MemberType.GetGenericArguments()[0];
            return $"CollectionPacker.ReadNullable<{valueType.VisibleClassName()}>(serializer)";
        }

        public string ReadingAsyncStatement(PropertyFieldInfo p)
        {
            var valueType = p.MemberType.GetGenericArguments()[0];
            return $"CollectionPacker.ReadNullableAsync<{valueType.VisibleClassName()}>(serializer)";
        }

        public string WrittingStatement(PropertyFieldInfo p)
        {
            var valueType = p.MemberType.GetGenericArguments()[0];
            return $"CollectionPacker.WriteNullable<{valueType.VisibleClassName()}>(value.{p.MemberName}, serializer);";
        }

        public string WrittingAsyncStatement(PropertyFieldInfo p)
        {
            var valueType = p.MemberType.GetGenericArguments()[0];
            return $"CollectionPacker.WriteNullableAsync<{valueType.VisibleClassName()}>(value.{p.MemberName}, serializer);";
        }
    }
}
