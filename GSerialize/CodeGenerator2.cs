using System;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Runtime.Loader;
using System.Threading.Tasks;

namespace GSerialize
{
    sealed class CodeGenerator2
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

        static void DumpCode(string code)
        {
            using var writter = new System.IO.StreamWriter("gen2.cs");
            writter.WriteLine(code);
        }

        public static Assembly CompileSerialable(
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

        private static string GenerateCode(List<Type> types)
        {
            var code = new List<String>();
            code.Add("using System;");
            code.Add("using System.IO;");            
            code.Add("using System.Collections.Generic;");
            code.Add("using System.Threading.Tasks;");
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

        private static List<string> GenerateCodeForType(Type type)
        {
            var code = new List<String>();
            code.Add($"public class {type.GeneratedClassName2()}");
            code.Add("{");
            code.AddRange(GenerateWriteMethod(type));
            code.AddRange(GenerateReadMethod(type));
            code.Add("}");

            return code;
        }        

        static readonly List<StatementGenerator2> s_statementGenerators = new List<StatementGenerator2>
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
            new SerializableStatementGenerator2(),
            new ArrayStatementGenerator2(),
            new ListStatementGenerator2(),
            new DictStatementGenerator2(),
            new EnumStatementGenerator2(),
            new NullableStatementGenerator2(),
        };

        private static List<string> GenerateWriteMethod(Type type)
        {
            var code = new List<string>();
            code.Add($"public static void Write({type.VisibleClassName()} value,");
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

            foreach (var p in FindProperties(type))
            {               
                code.Add("{"); 
                code.AddRange(GeneratePropertyWrite(p));
                code.Add("}");
            }
            code.Add("}");

            code.Add($"public static async Task WriteAsync({type.VisibleClassName()} value,");
            code.Add("Serializer2 serializer, Dictionary<Object,int> cache)");
            code.Add("{");
            code.Add("var packer = serializer.Packer;");
            code.Add($"if (value == null)");
            code.Add("{ packer.WriteInt32(0); return; }");
            code.Add("{");
            code.Add("if (cache.TryGetValue(value, out int id)) {");
            code.Add("await packer.WriteInt32Async(id);");
            code.Add("return;");
            code.Add("}}");
            code.Add("{");
            code.Add("var id = cache.Count + 1;");
            code.Add("cache[value] = id;");
            code.Add("await packer.WriteInt32Async(id);");
            code.Add("}");

            foreach (var p in FindProperties(type))
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

        private static List<string> GeneratePropertyWrite(PropertyFieldInfo p)
        {
            foreach(var gen in s_statementGenerators)
            {
                if (gen.Matches(p))
                {
                    return WrittingCode(p, gen.WrittingStatement(p), gen.NeedCheckReference);
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
                    return WrittingAsyncCode(p, gen.WrittingAsyncStatement(p), gen.NeedCheckReference);
                }
            }
            throw new NotSupportedException($"{p.MemberType} of {p.MemberName} is not a supported type");
        }

        private static List<string> GenerateReadMethod(Type type)
        {
            var code = new List<string>();
            var ReturnClassName = type.VisibleClassName();
            code.Add($"public static {ReturnClassName} Read(Serializer2 serializer, Dictionary<int, Object> cache)");
            code.Add("{");
            code.Add("var packer = serializer.Packer;");
            code.Add("var refId = packer.ReadInt32();");
            code.Add("if (refId == 0 ) return null;");
            code.Add($"if (cache.TryGetValue(refId, out object ret)) return ({ReturnClassName})ret;");
            code.Add($"var result = new {ReturnClassName}();");
            code.Add("cache[refId] = result;");
            
            foreach (var p in FindProperties(type))
            {                
                code.Add("{");
                code.AddRange(GeneratePropertyRead(p));
                code.Add("};");
            }
            code.Add("return result;");
            code.Add("}");

            code.Add($"public static async Task<{ReturnClassName}> ReadAsync(Serializer2 serializer, Dictionary<int, Object> cache)");
            code.Add("{");
            code.Add("var packer = serializer.Packer;");
            code.Add("var refId = await packer.ReadInt32Async();");
            code.Add("if (refId == 0 ) return null;");
            code.Add($"if (cache.TryGetValue(refId, out object ret)) return ({ReturnClassName})ret;");
            code.Add($"var result = new {ReturnClassName}();");
            code.Add("cache[refId] = result;");
            
            foreach (var p in FindProperties(type))
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

        private static List<string> GeneratePropertyRead(PropertyFieldInfo p)
        {
            foreach(var gen in s_statementGenerators)
            {
                if (gen.Matches(p))
                {
                    return ReadingCode(p, gen.ReadingStatement(p), gen.NeedCheckReference);
                }
            }
            throw new NotSupportedException($"{p.MemberType} of {p.MemberName} is not a supported type");
        }

        private static List<string> GeneratePropertyAsyncRead(PropertyFieldInfo p)
        {
            foreach(var gen in s_statementGenerators)
            {
                if (gen.Matches(p))
                {
                    return ReadingAsyncCode(p, gen.ReadingAsyncStatement(p), gen.NeedCheckReference);
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

    static class TypeExtension2
    {
        internal static string GeneratedClassName2(this Type type)
        {
            return $"Serial2_{type.FullName.Replace('.', '_').Replace('+', '_')}";
        }

        internal static string GeneratedFullClassName2(this Type type)
        {
            return $"GSerialize.Generated.{type.GeneratedClassName2()}";
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

    class SerializableStatementGenerator2 : StatementGenerator2
    {
        public bool Matches(PropertyFieldInfo p)
        {
            return p.MemberType.IsDefined(typeof(GSerializableAttribute), inherit: false);
        }

        public bool NeedCheckReference => true;

        public string ReadingStatement(PropertyFieldInfo p)
        {
            return $"{p.MemberType.GeneratedClassName2()}.Read(serializer, cache)";
        }

        public string ReadingAsyncStatement(PropertyFieldInfo p)
        {
            return $"{p.MemberType.GeneratedClassName2()}.ReadAsync(serializer, cache)";
        }

        public string WrittingStatement(PropertyFieldInfo p)
        {
            return $"{p.MemberType.GeneratedClassName2()}.Write(value.{p.MemberName}, serializer, cache);";
        }

        public string WrittingAsyncStatement(PropertyFieldInfo p)
        {
            return $"{p.MemberType.GeneratedClassName2()}.WriteAsync(value.{p.MemberName}, serializer, cache);";
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
            return $"CollectionPacker2.ReadStringAsync(serializer, cache)";
        }

        public string WrittingStatement(PropertyFieldInfo p)
        {
            return $"CollectionPacker2.WriteString(value.{p.MemberName}, serializer, cache);";
        }

        public string WrittingAsyncStatement(PropertyFieldInfo p)
        {
            return $"CollectionPacker2.WriteStringAsync(value.{p.MemberName}, serializer, cache);";
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
            var itemType = p.MemberType.GetGenericArguments()[0];
            return $"CollectionPacker2.ReadList<{itemType.VisibleClassName()}>(serializer, cache)";
        }

        public string ReadingAsyncStatement(PropertyFieldInfo p)
        {
            var itemType = p.MemberType.GetGenericArguments()[0];
            return $"CollectionPacker2.ReadListAsync<{itemType.VisibleClassName()}>(serializer, cache)";
        }

        public string WrittingStatement(PropertyFieldInfo p)
        {
            var itemType = p.MemberType.GetGenericArguments()[0];
            return $"CollectionPacker2.WriteList<{itemType.VisibleClassName()}>(value.{p.MemberName}, serializer, cache);";
        }

        public string WrittingAsyncStatement(PropertyFieldInfo p)
        {
            var itemType = p.MemberType.GetGenericArguments()[0];
            return $"CollectionPacker2.WriteListAsync<{itemType.VisibleClassName()}>(value.{p.MemberName}, serializer, cache);";
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
            var elementType = p.MemberType.GetElementType();
            return $"CollectionPacker2.ReadArray<{elementType.VisibleClassName()}>(serializer, cache)";
        }

        public string ReadingAsyncStatement(PropertyFieldInfo p)
        {
            var elementType = p.MemberType.GetElementType();
            return $"CollectionPacker2.ReadArrayAsync<{elementType.VisibleClassName()}>(serializer, cache)";
        }

        public string WrittingStatement(PropertyFieldInfo p)
        {
            var elementType = p.MemberType.GetElementType();
            return $"CollectionPacker2.WriteArray<{elementType.VisibleClassName()}>(value.{p.MemberName}, serializer, cache);";
        }

        public string WrittingAsyncStatement(PropertyFieldInfo p)
        {
            var elementType = p.MemberType.GetElementType();
            return $"CollectionPacker2.WriteArrayAsync<{elementType.VisibleClassName()}>(value.{p.MemberName}, serializer, cache);";
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
            var keyType = p.MemberType.GetGenericArguments()[0];
            var valueType = p.MemberType.GetGenericArguments()[1];
            return $"CollectionPacker2.ReadDict<{keyType.VisibleClassName()},{valueType.VisibleClassName()}>(serializer, cache)";
        }

        public string ReadingAsyncStatement(PropertyFieldInfo p)
        {
            var keyType = p.MemberType.GetGenericArguments()[0];
            var valueType = p.MemberType.GetGenericArguments()[1];
            return $"CollectionPacker2.ReadDictAsync<{keyType.VisibleClassName()},{valueType.VisibleClassName()}>(serializer, cache)";
        }

        public string WrittingStatement(PropertyFieldInfo p)
        {
            var keyType = p.MemberType.GetGenericArguments()[0];
            var valueType = p.MemberType.GetGenericArguments()[1];
            return $"CollectionPacker2.WriteDict<{keyType.VisibleClassName()},{valueType.VisibleClassName()}>(value.{p.MemberName}, serializer, cache);";
        }

        public string WrittingAsyncStatement(PropertyFieldInfo p)
        {
            var keyType = p.MemberType.GetGenericArguments()[0];
            var valueType = p.MemberType.GetGenericArguments()[1];
            return $"CollectionPacker2.WriteDictAsync<{keyType.VisibleClassName()},{valueType.VisibleClassName()}>(value.{p.MemberName}, serializer, cache);";
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
            return $"CollectionPacker2.ReadEnum<{p.MemberType.VisibleClassName()}>(serializer)";
        }

        public string ReadingAsyncStatement(PropertyFieldInfo p)
        {
            return $"CollectionPacker2.ReadEnumAsync<{p.MemberType.VisibleClassName()}>(serializer)";
        }

        public string WrittingStatement(PropertyFieldInfo p)
        {
            return $"CollectionPacker2.WriteEnum<{p.MemberType.VisibleClassName()}>(value.{p.MemberName}, serializer);";
        }

        public string WrittingAsyncStatement(PropertyFieldInfo p)
        {
            return $"CollectionPacker2.WriteEnumAsync<{p.MemberType.VisibleClassName()}>(value.{p.MemberName}, serializer);";
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
            var valueType = p.MemberType.GetGenericArguments()[0];
            return $"CollectionPacker2.ReadNullable<{valueType.VisibleClassName()}>(serializer)";
        }

        public string ReadingAsyncStatement(PropertyFieldInfo p)
        {
            var valueType = p.MemberType.GetGenericArguments()[0];
            return $"CollectionPacker2.ReadNullableAsync<{valueType.VisibleClassName()}>(serializer)";
        }

        public string WrittingStatement(PropertyFieldInfo p)
        {
            var valueType = p.MemberType.GetGenericArguments()[0];
            return $"CollectionPacker2.WriteNullable<{valueType.VisibleClassName()}>(value.{p.MemberName}, serializer);";
        }

        public string WrittingAsyncStatement(PropertyFieldInfo p)
        {
            var valueType = p.MemberType.GetGenericArguments()[0];
            return $"CollectionPacker2.WriteNullableAsync<{valueType.VisibleClassName()}>(value.{p.MemberName}, serializer);";
        }
    }
}
