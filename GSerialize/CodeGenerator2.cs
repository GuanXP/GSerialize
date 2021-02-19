/*
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
using System.Net;

namespace GSerialize
{
    sealed class CodeGenerator2
    {
        void LoadReferencedAssemblies(Assembly assembly, 
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

        static void DumpCode(string code)
        {
            using var writter = new System.IO.StreamWriter("gen2.cs");
            writter.WriteLine(code);
        }

        public Assembly CompileSerialable(
            List<Type> types, 
            List<Assembly> referencedAssemblies,
            string generatedAssemblyName)
        {
            var code = GenerateCode(types);
            //DumpCode(code);
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

        private string GenerateCode(List<Type> types)
        {
            var code = new List<String>();
            code.Add("using System;");
            code.Add("using System.IO;");            
            code.Add("using System.Collections.Generic;");
            code.Add("using System.Threading.Tasks;");
            code.Add("using System.Threading;");
            code.Add("using GSerialize;");
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
                builder.AppendLine(s);
            }
            return builder.ToString();
        }

        private List<string> GenerateCodeForType(Type type)
        {
            var code = new List<String>();
            code.Add($"public sealed class {type.GeneratedClassName2()}");
            code.Add("{");
            code.AddRange(GenerateWriteMethod(type));
            code.AddRange(GenerateReadMethod(type));
            code.Add("}");

            return code;
        }        

        readonly List<StatementGenerator2> _statementGenerators = new List<StatementGenerator2>
        {
            new StringStatementGenerator2(),
            new PrimitiveStatementGenerator2(typeof(UInt16), "UInt16"),
            new PrimitiveStatementGenerator2(typeof(Int16), "Int16"),
            new PrimitiveStatementGenerator2(typeof(UInt32), "UInt32"),
            new PrimitiveStatementGenerator2(typeof(Int32), "Int32"),
            new PrimitiveStatementGenerator2(typeof(UInt64), "UInt64"),
            new PrimitiveStatementGenerator2(typeof(Int64), "Int64"),
            new PrimitiveStatementGenerator2(typeof(Byte), "Byte"),
            new PrimitiveStatementGenerator2(typeof(SByte), "SByte"),
            new PrimitiveStatementGenerator2(typeof(Boolean), "Bool"),
            new PrimitiveStatementGenerator2(typeof(Char), "Char"),
            new PrimitiveStatementGenerator2(typeof(double), "Double"),
            new PrimitiveStatementGenerator2(typeof(float), "Float"),
            new PrimitiveStatementGenerator2(typeof(decimal), "Decimal"),
            new PrimitiveStatementGenerator2(typeof(DateTime), "DateTime"),
            new PrimitiveStatementGenerator2(typeof(TimeSpan), "TimeSpan"),
            new PrimitiveStatementGenerator2(typeof(Guid), "Guid"),
            new PrimitiveStatementGenerator2(typeof(IPEndPoint), "IPEndPoint"),
            new SerializableStatementGenerator2(),
            new ArrayStatementGenerator2(),
            new ListStatementGenerator2(),
            new DictStatementGenerator2(),
            new EnumStatementGenerator2(),
            new NullableStatementGenerator2(),
        };

        private List<string> GenerateWriteMethod(Type type)
        {
            var code = new List<string>();
            code.Add($"public static void Write({type.CompilableClassName()} value,");
            code.Add("Serializer2 serializer, Dictionary<Object,int> cache)");
            code.Add("{");
            code.Add("var packer = serializer.Packer;");
            code.Add($"if (value == null)");
            code.Add("{ packer.WriteInt32(0); return; }");
            code.Add("{");
            code.Add("if (cache.TryGetValue(value, out int id)) {");
            code.Add("packer.WriteInt32(id);");
            code.Add("return;");
            code.Add("}}");
            code.Add("{");
            code.Add("var id = cache.Count + 1;");
            code.Add("cache[value] = id;");
            code.Add("packer.WriteInt32(id);");
            code.Add("}");

            foreach (var p in PropertyFieldInfo.FindProperties(type))
            {               
                code.Add("{"); 
                code.AddRange(GeneratePropertyWrite(p));
                code.Add("}");
            }
            code.Add("}");

            code.Add($"public static async Task WriteAsync({type.CompilableClassName()} value,");
            code.Add("Serializer2 serializer, Dictionary<Object,int> cache, CancellationToken cancellation)");
            code.Add("{");
            code.Add("var packer = serializer.Packer;");
            code.Add($"if (value == null)");
            code.Add("{ await packer.WriteInt32Async(0, cancellation); return; }");
            code.Add("{");
            code.Add("if (cache.TryGetValue(value, out int id)) {");
            code.Add("await packer.WriteInt32Async(id, cancellation);");
            code.Add("return;");
            code.Add("}}");
            code.Add("{");
            code.Add("var id = cache.Count + 1;");
            code.Add("cache[value] = id;");
            code.Add("await packer.WriteInt32Async(id, cancellation);");
            code.Add("}");

            foreach (var p in PropertyFieldInfo.FindProperties(type))
            {                
                code.Add("{");
                code.AddRange(GeneratePropertyAsyncWrite(p));
                code.Add("}");
            }
            code.Add("}");
            return code;
        }

        private static List<string> WrittingCode(
            PropertyFieldInfo p, string statementWrite,
            bool needCheckReference)
        {
            var code = new List<string>();
            if (!needCheckReference)
            {
                code.Add(statementWrite);
                return code;           
            }
            
            if (p.IsOptional)
            {
                code.Add(statementWrite);
            }
            else
            {   
                code.Add($"if (value.{p.MemberName} == null)");
                code.Add("{");
                code.Add($"throw new ArgumentNullException(\"{p.MemberName}\");");            
                code.Add("}");
                code.Add(statementWrite);                
            }            
            
            return code;
        }

        private static List<string> WrittingAsyncCode(
            PropertyFieldInfo p, string statementWrite,
            bool needCheckReference)
        {
            var code = new List<string>();
            if (!needCheckReference)
            {
                code.Add($"await {statementWrite}");
                return code;           
            }
            
            if (p.IsOptional)
            {
                code.Add($"await {statementWrite}");
            }
            else
            {   
                code.Add($"if (value.{p.MemberName} == null)");
                code.Add("{");
                code.Add($"throw new ArgumentNullException(\"{p.MemberName}\");");            
                code.Add("}");
                code.Add($"await {statementWrite}");                
            }            
            return code;
        }

        private List<string> GeneratePropertyWrite(PropertyFieldInfo p)
        {
            foreach(var gen in _statementGenerators)
            {
                if (gen.Matches(p))
                {
                    return WrittingCode(p, gen.WrittingStatement(p), gen.NeedCheckReference);
                }
            }
            throw new NotSupportedException($"{p.MemberType} of {p.MemberName} is not a supported type");
        }

        private List<string> GeneratePropertyAsyncWrite(PropertyFieldInfo p)
        {
            foreach(var gen in _statementGenerators)
            {
                if (gen.Matches(p))
                {
                    return WrittingAsyncCode(p, gen.WrittingAsyncStatement(p), gen.NeedCheckReference);
                }
            }
            throw new NotSupportedException($"{p.MemberType} of {p.MemberName} is not a supported type");
        }

        private List<string> GenerateReadMethod(Type type)
        {
            var code = new List<string>();
            var ReturnClassName = type.CompilableClassName();
            code.Add($"public static {ReturnClassName} Read(Serializer2 serializer, Dictionary<int, Object> cache)");
            code.Add("{");
            code.Add("var packer = serializer.Packer;");
            code.Add("var refId = packer.ReadInt32();");
            code.Add("if (refId == 0 ) return null;");
            code.Add($"if (cache.TryGetValue(refId, out object ret)) return ({ReturnClassName})ret;");
            code.Add($"var result = new {ReturnClassName}();");
            code.Add("cache[refId] = result;");
            
            foreach (var p in PropertyFieldInfo.FindProperties(type))
            {                
                code.Add("{");
                code.AddRange(GeneratePropertyRead(p));
                code.Add("};");
            }
            code.Add("return result;");
            code.Add("}");

            code.Add($"public static async Task<{ReturnClassName}> ReadAsync(Serializer2 serializer,");
            code.Add("Dictionary<int, Object> cache, CancellationToken cancellation)");
            code.Add("{");
            code.Add("var packer = serializer.Packer;");
            code.Add("var refId = await packer.ReadInt32Async(cancellation);");
            code.Add("if (refId == 0 ) return null;");
            code.Add($"if (cache.TryGetValue(refId, out object ret)) return ({ReturnClassName})ret;");
            code.Add($"var result = new {ReturnClassName}();");
            code.Add("cache[refId] = result;");
            
            foreach (var p in PropertyFieldInfo.FindProperties(type))
            {                
                code.Add("{");
                code.AddRange(GeneratePropertyAsyncRead(p));
                code.Add("};");
            }
            code.Add("return result;");
            code.Add("}");
            return code;
        }

        private static List<string> ReadingCode(
            PropertyFieldInfo p, string statementRead,
            bool needCheckReference)
        {
            var code  = new List<string>();
            if (!needCheckReference)
            {
                code.Add($"result.{p.MemberName} = {statementRead};");
                return code;
            }

            if (p.IsOptional)
            {
                code.Add($"result.{p.MemberName} = {statementRead};");
            } else {
                code.Add($"var value = {statementRead};");
                code.Add("if (value == null ) {");
                code.Add($"throw new InvalidDataException(\"required member {p.MemberName} meets null data\");");
                code.Add("}");
                code.Add($"result.{p.MemberName} = value;");
            }
            return code;
        }

        private static List<string> ReadingAsyncCode(
            PropertyFieldInfo p, string statementRead,
            bool needCheckReference)
        {
            var code  = new List<string>();
            if (!needCheckReference)
            {
                code.Add($"result.{p.MemberName} = await {statementRead};");
                return code;
            }

            if (p.IsOptional)
            {
                code.Add($"result.{p.MemberName} = await {statementRead};");
            } else {
                code.Add($"var value = await {statementRead};");
                code.Add("if (value == null ) {");
                code.Add($"throw new InvalidDataException(\"required member {p.MemberName} meets null data\");");
                code.Add("}");
                code.Add($"result.{p.MemberName} = value;");
            }
            return code;
        }

        private List<string> GeneratePropertyRead(PropertyFieldInfo p)
        {
            foreach(var gen in _statementGenerators)
            {
                if (gen.Matches(p))
                {
                    return ReadingCode(p, gen.ReadingStatement(p), gen.NeedCheckReference);
                }
            }
            throw new NotSupportedException($"{p.MemberType} of {p.MemberName} is not a supported type");
        }

        private List<string> GeneratePropertyAsyncRead(PropertyFieldInfo p)
        {
            foreach(var gen in _statementGenerators)
            {
                if (gen.Matches(p))
                {
                    return ReadingAsyncCode(p, gen.ReadingAsyncStatement(p), gen.NeedCheckReference);
                }
            }
            throw new NotSupportedException($"{p.MemberType} of {p.MemberName} is not a supported type");
        }        
    }

    interface StatementGenerator2
    {
        bool Matches(PropertyFieldInfo p);
        bool NeedCheckReference {get;}
        string ReadingStatement(PropertyFieldInfo p);
        string WrittingStatement(PropertyFieldInfo p);
        string ReadingAsyncStatement(PropertyFieldInfo p);
        string WrittingAsyncStatement(PropertyFieldInfo p);
    }

    class PrimitiveStatementGenerator2 : StatementGenerator2
    {
        private readonly string _packerTypeName;
        private readonly Type _matchedType;
        internal PrimitiveStatementGenerator2(Type matchedType, string packerTypeName)
        {
            _packerTypeName = packerTypeName;
            _matchedType = matchedType;
        }

        public bool Matches(PropertyFieldInfo p)
        {
            return p.MemberType == _matchedType;
        }

        public bool NeedCheckReference  => false;

        public string ReadingStatement(PropertyFieldInfo p)
        {
            return $"packer.Read{_packerTypeName}()";
        }

        public string ReadingAsyncStatement(PropertyFieldInfo p)
        {
            return $"packer.Read{_packerTypeName}Async(cancellation)";
        }

        public string WrittingStatement(PropertyFieldInfo p)
        {
            return $"packer.Write{_packerTypeName}(value.{p.MemberName});";
        }

        public string WrittingAsyncStatement(PropertyFieldInfo p)
        {
            return $"packer.Write{_packerTypeName}Async(value.{p.MemberName}, cancellation);";
        }
    }

    class SerializableStatementGenerator2 : StatementGenerator2
    {
        public bool Matches(PropertyFieldInfo p)
        {
            return p.MemberType.IsSerializableClass();
        }

        public bool NeedCheckReference => true;

        public string ReadingStatement(PropertyFieldInfo p)
        {
            return $"{p.MemberType.GeneratedClassName2()}.Read(serializer, cache)";
        }

        public string ReadingAsyncStatement(PropertyFieldInfo p)
        {
            return $"{p.MemberType.GeneratedClassName2()}.ReadAsync(serializer, cache, cancellation)";
        }

        public string WrittingStatement(PropertyFieldInfo p)
        {
            return $"{p.MemberType.GeneratedClassName2()}.Write(value.{p.MemberName}, serializer, cache);";
        }

        public string WrittingAsyncStatement(PropertyFieldInfo p)
        {
            return $"{p.MemberType.GeneratedClassName2()}.WriteAsync(value.{p.MemberName}, serializer, cache, cancellation);";
        }
    }

    class StringStatementGenerator2 : StatementGenerator2
    {
        public bool Matches(PropertyFieldInfo p)
        {
            return p.MemberType == typeof(string);
        }

        public bool NeedCheckReference => true;

        public string ReadingStatement(PropertyFieldInfo p)
        {
            return $"CollectionPacker2.ReadString(serializer, cache)";
        }

        public string ReadingAsyncStatement(PropertyFieldInfo p)
        {
            return $"CollectionPacker2.ReadStringAsync(serializer, cache, cancellation)";
        }

        public string WrittingStatement(PropertyFieldInfo p)
        {
            return $"CollectionPacker2.WriteString(value.{p.MemberName}, serializer, cache);";
        }

        public string WrittingAsyncStatement(PropertyFieldInfo p)
        {
            return $"CollectionPacker2.WriteStringAsync(value.{p.MemberName}, serializer, cache, cancellation);";
        }
    }

    class ListStatementGenerator2 : StatementGenerator2
    {
        public bool Matches(PropertyFieldInfo p)
        {
            return p.MemberType.Name == "List`1" && p.MemberType.IsGenericType;
        }

        public bool NeedCheckReference => true;

        public string ReadingStatement(PropertyFieldInfo p)
        {
            return $"CollectionPacker2.ReadList{p.GenericParams()}(serializer, cache)";
        }

        public string ReadingAsyncStatement(PropertyFieldInfo p)
        {
            return $"CollectionPacker2.ReadListAsync{p.GenericParams()}(serializer, cache, cancellation)";
        }

        public string WrittingStatement(PropertyFieldInfo p)
        {
            return $"CollectionPacker2.WriteList{p.GenericParams()}(value.{p.MemberName}, serializer, cache);";
        }

        public string WrittingAsyncStatement(PropertyFieldInfo p)
        {
            return $"CollectionPacker2.WriteListAsync{p.GenericParams()}(value.{p.MemberName}, serializer, cache, cancellation);";
        }
    }

    class ArrayStatementGenerator2 : StatementGenerator2
    {
        public bool Matches(PropertyFieldInfo p)
        {
            return p.MemberType.IsArray && p.MemberType.GetArrayRank() == 1 && p.MemberType.HasElementType;
        }

        public bool NeedCheckReference => true;

        public string ReadingStatement(PropertyFieldInfo p)
        {
            return $"CollectionPacker2.ReadArray{p.GenericParams()}(serializer, cache)";
        }

        public string ReadingAsyncStatement(PropertyFieldInfo p)
        {
            return $"CollectionPacker2.ReadArrayAsync{p.GenericParams()}(serializer, cache, cancellation)";
        }

        public string WrittingStatement(PropertyFieldInfo p)
        {
            return $"CollectionPacker2.WriteArray{p.GenericParams()}(value.{p.MemberName}, serializer, cache);";
        }

        public string WrittingAsyncStatement(PropertyFieldInfo p)
        {
            return $"CollectionPacker2.WriteArrayAsync{p.GenericParams()}(value.{p.MemberName}, serializer, cache, cancellation);";
        }
    }

    class DictStatementGenerator2 : StatementGenerator2
    {
        public bool Matches(PropertyFieldInfo p)
        {
            return p.MemberType.Name == "Dictionary`2" && p.MemberType.IsGenericType;
        }

        public bool NeedCheckReference => true;

        public string ReadingStatement(PropertyFieldInfo p)
        {
            return $"CollectionPacker2.ReadDict{p.GenericParams()}(serializer, cache)";
        }

        public string ReadingAsyncStatement(PropertyFieldInfo p)
        {
            return $"CollectionPacker2.ReadDictAsync{p.GenericParams()}(serializer, cache, cancellation)";
        }

        public string WrittingStatement(PropertyFieldInfo p)
        {
            return $"CollectionPacker2.WriteDict{p.GenericParams()}(value.{p.MemberName}, serializer, cache);";
        }

        public string WrittingAsyncStatement(PropertyFieldInfo p)
        {
            return $"CollectionPacker2.WriteDictAsync{p.GenericParams()}(value.{p.MemberName}, serializer, cache, cancellation);";
        }
    }

    class EnumStatementGenerator2 : StatementGenerator2
    {
        public bool Matches(PropertyFieldInfo p)
        {
            return p.MemberType.IsEnum;
        }

        public bool NeedCheckReference => false;

        public string ReadingStatement(PropertyFieldInfo p)
        {
            return $"CollectionPacker2.ReadEnum<{p.MemberType.CompilableClassName()}>(serializer)";
        }

        public string ReadingAsyncStatement(PropertyFieldInfo p)
        {
            return $"CollectionPacker2.ReadEnumAsync<{p.MemberType.CompilableClassName()}>(serializer, cancellation)";
        }

        public string WrittingStatement(PropertyFieldInfo p)
        {
            return $"CollectionPacker2.WriteEnum<{p.MemberType.CompilableClassName()}>(value.{p.MemberName}, serializer);";
        }

        public string WrittingAsyncStatement(PropertyFieldInfo p)
        {
            return $"CollectionPacker2.WriteEnumAsync<{p.MemberType.CompilableClassName()}>(value.{p.MemberName}, serializer, cancellation);";
        }
    }

    class NullableStatementGenerator2 : StatementGenerator2
    {
        public bool Matches(PropertyFieldInfo p)
        {
            return p.MemberType.Name == "Nullable`1" && p.MemberType.IsGenericType;
        }

        public bool NeedCheckReference => false;

        public string ReadingStatement(PropertyFieldInfo p)
        {
            return $"CollectionPacker2.ReadNullable{p.GenericParams()}(serializer)";
        }

        public string ReadingAsyncStatement(PropertyFieldInfo p)
        {
            return $"CollectionPacker2.ReadNullableAsync{p.GenericParams()}(serializer, cancellation)";
        }

        public string WrittingStatement(PropertyFieldInfo p)
        {
            return $"CollectionPacker2.WriteNullable{p.GenericParams()}(value.{p.MemberName}, serializer);";
        }

        public string WrittingAsyncStatement(PropertyFieldInfo p)
        {
            return $"CollectionPacker2.WriteNullableAsync{p.GenericParams()}(value.{p.MemberName}, serializer, cancellation);";
        }
    }
}
