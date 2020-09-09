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
        public static string ClassNameGenerated(Type commandType)
        {
            return $"Serial_{commandType.FullName.Replace('.', '_').Replace('+', '_')}";
        }

        public static string FullClassNameGenerated(Type commandType)
        {
            return $"GSerialize.Generated.{ClassNameGenerated(commandType)}";
        }

        static void LoadReferencedAssemblies(Assembly assembly, 
            List<MetadataReference> references, List<Assembly> loadeAssemblies)
        {
            if (loadeAssemblies.FirstOrDefault(x => x == assembly) != null) return;

            loadeAssemblies.Add(assembly);
            references.Add(MetadataReference.CreateFromFile(assembly.Location));
            foreach (var refName in assembly.GetReferencedAssemblies())
            {
                var refAssembly = Assembly.Load(refName);
                LoadReferencedAssemblies(refAssembly, references, loadeAssemblies);
            }
        }

        public static Assembly CompileSerialable(
            List<Type> types, 
            List<Assembly> referencedAsseblies,
            string generatedAssemblyName)
        {
            var code = GenerateCode(types);
            var tree = SyntaxFactory.ParseSyntaxTree(code);

            var references = new List<MetadataReference>();
            foreach(var a in referencedAsseblies)
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
            code.Add(@"
using System.IO;
using GSerialize;
namespace GSerialize.Generated
{"
            );

            foreach(var t in types)
            {
                var classCode = GenerateCodeForType(t);
                code.Add(classCode);
            }

            code.Add("} //end of namespace");

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

        private static string GenerateCodeForType(Type type)
        {
            var code = new List<String>();
            code.Add($"public class {ClassNameGenerated(type)}");
            code.Add("{");
            code.AddRange(GenerateWriteMethod(type));
            code.AddRange(GenerateReadMethod(type));
            code.Add("} //end of class");

            return CodeLinesToString(code);
        }

        private static string FullClassName(Type type)
        {
            return type.FullName.Replace('+', '.');
        }

        private static List<string> GenerateWriteMethod(Type type)
        {
            var code = new List<string>();
            code.Add($"public static void Write({FullClassName(type)} value, Serializer serializer)");
            code.Add("{");
            code.Add("var packer = serializer.Packer;");
            foreach (var p in FindProperties(type))
            {                
                code.AddRange(GeneratePropertyWrite(p));
            }
            code.Add("} // end of Write");
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

        private static List<string> GeneratePropertyWrite(PropertyFieldInfo p)
        {
            var code = new List<string>();
            if (p.MemberType == typeof(string))
            {
                var statementWrite = $"packer.WriteString(value.{p.MemberName});";
                code.AddRange(WrittingCode(p, statementWrite));
            }
            else if (p.MemberType == typeof(Boolean))
            {
                var statementWrite = $"packer.WriteBool(value.{p.MemberName});";
                code.AddRange(WrittingCode(p, statementWrite));
            }
            else if (p.MemberType == typeof(Int64))
            {
                var statementWrite = $"packer.WriteInt64(value.{p.MemberName});";
                code.AddRange(WrittingCode(p, statementWrite));
            }
            else if (p.MemberType == typeof(UInt64))
            {
                var statementWrite = $"packer.WriteUInt64(value.{p.MemberName});";
                code.AddRange(WrittingCode(p, statementWrite));
            }
            else if (p.MemberType == typeof(Int32))
            {
                var statementWrite = $"packer.WriteInt32(value.{p.MemberName});";
                code.AddRange(WrittingCode(p, statementWrite));
            }
            else if (p.MemberType == typeof(UInt32))
            {
                var statementWrite = $"packer.WriteUInt32(value.{p.MemberName});";
                code.AddRange(WrittingCode(p, statementWrite));
            }
            else if (p.MemberType == typeof(Int16))
            {
                var statementWrite = $"packer.WriteInt16(value.{p.MemberName});";
                code.AddRange(WrittingCode(p, statementWrite));
            }
            else if (p.MemberType == typeof(UInt16))
            {
                var statementWrite = $"packer.WriteUInt16(value.{p.MemberName});";
                code.AddRange(WrittingCode(p, statementWrite));
            }
            else if (p.MemberType == typeof(double))
            {
                var statementWrite = $"packer.WriteDouble(value.{p.MemberName});";
                code.AddRange(WrittingCode(p, statementWrite));
            }
            else if (p.MemberType == typeof(float))
            {
                var statementWrite = $"packer.WriteFloat(value.{p.MemberName});";
                code.AddRange(WrittingCode(p, statementWrite));
            }
            else if (p.MemberType == typeof(Char))
            {
                var statementWrite = $"packer.WriteChar(value.{p.MemberName});";
                code.AddRange(WrittingCode(p, statementWrite));
            }
            else if (p.MemberType == typeof(Decimal))
            {
                var statementWrite = $"packer.WriteDecimal(value.{p.MemberName});";
                code.AddRange(WrittingCode(p, statementWrite));
            }
            else if (p.MemberType == typeof(Guid))
            {
                var statementWrite = $"packer.WriteGuid(value.{p.MemberName});";
                code.AddRange(WrittingCode(p, statementWrite));
            }
            else if (p.MemberType == typeof(DateTime))
            {
                var statementWrite = $"packer.WriteDateTime(value.{p.MemberName});";
                code.AddRange(WrittingCode(p, statementWrite));
            }
            else if (p.MemberType.IsDefined(typeof(GSerializableAttribute), inherit: false))
            {
                var statementWrite = $"{ClassNameGenerated(p.MemberType)}.Write(value.{p.MemberName}, serializer);";
                code.AddRange(WrittingCode(p, statementWrite));
            }
            else if (p.MemberType.Name =="List`1" && p.MemberType.IsGenericType)
            {
                var itemType = p.MemberType.GetGenericArguments()[0];
                var statementWrite = $"CollectionPacker.WriteList<{FullClassName(itemType)}>(value.{p.MemberName}, serializer);";
                code.AddRange(WrittingCode(p, statementWrite));
            }
            else if (p.MemberType.Name == "Nullable`1" && p.MemberType.IsGenericType)
            {
                var valueType = p.MemberType.GetGenericArguments()[0];
                var statementWrite = $"CollectionPacker.WriteNullable<{FullClassName(valueType)}>(value.{p.MemberName}, serializer);";
                code.AddRange(WrittingCode(p, statementWrite));
            }
            else if (p.MemberType.Name == "Dictionary`2" && p.MemberType.IsGenericType)
            {
                var keyType = p.MemberType.GetGenericArguments()[0];
                var valueType = p.MemberType.GetGenericArguments()[1];
                var statementWrite = $"CollectionPacker.WriteDict<{FullClassName(keyType)},{FullClassName(valueType)}>(value.{p.MemberName}, serializer);";
                code.AddRange(WrittingCode(p, statementWrite));
            }
            else if (p.MemberType.IsArray && p.MemberType.HasElementType)
            {
                var elementType = p.MemberType.GetElementType();
                var statementWrite = $"CollectionPacker.WriteArray<{FullClassName(elementType)}>(value.{p.MemberName}, serializer);";
                code.AddRange(WrittingCode(p, statementWrite));
            }
            else if (p.MemberType.IsEnum)
            {
                var statementWrite = $"CollectionPacker.WriteEnum<{FullClassName(p.MemberType)}>(value.{p.MemberName}, serializer);";
                code.AddRange(WrittingCode(p, statementWrite));
            }
            else
            {
                throw new NotSupportedException($"{p.MemberType} of {p.MemberName} is not a supported type");
            }
            return code;
        }

        private static List<string> GenerateReadMethod(Type type)
        {
            var code = new List<string>();
            var fullTypeName = FullClassName(type);
            code.Add($"public static {fullTypeName} Read(Serializer serializer)");
            code.Add("{");
            code.Add("var packer = serializer.Packer;");           
            code.Add($"return new {fullTypeName}");
            code.Add("{");
            foreach (var p in FindProperties(type))
            {                
                code.AddRange(GeneratePropertyRead(p));
            }
            code.Add("};");
            code.Add("} // end of Read");
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

        private static List<string> GeneratePropertyRead(PropertyFieldInfo p)
        {
            var code = new List<string>();
            if (p.MemberType == typeof(string))
            {
                code.Add(ReadingCode(p, "packer.ReadString()"));
            }
            else if (p.MemberType == typeof(Boolean))
            {
                code.Add(ReadingCode(p, "packer.ReadBool()"));
            }
            else if (p.MemberType == typeof(Int64))
            {
                code.Add(ReadingCode(p, "packer.ReadInt64()"));
            }
            else if (p.MemberType == typeof(UInt64))
            {
                code.Add(ReadingCode(p, "packer.ReadUInt64()"));
            }
            else if (p.MemberType == typeof(Int32))
            {
                code.Add(ReadingCode(p, "packer.ReadInt32()"));
            }
            else if (p.MemberType == typeof(UInt32))
            {
                code.Add(ReadingCode(p, "packer.ReadUInt32()"));
            }
            else if (p.MemberType == typeof(Int16))
            {
                code.Add(ReadingCode(p, "packer.ReadInt16()"));
            }
            else if (p.MemberType == typeof(UInt16))
            {
                code.Add(ReadingCode(p, "packer.ReadUInt16()"));
            }
            else if (p.MemberType == typeof(double))
            {
                code.Add(ReadingCode(p, "packer.ReadDouble()"));
            }
            else if (p.MemberType == typeof(float))
            {
                code.Add(ReadingCode(p, "packer.ReadFloat()"));
            }
            else if (p.MemberType == typeof(DateTime))
            {
                code.Add(ReadingCode(p, "packer.ReadDateTime()"));
            }
            else if (p.MemberType == typeof(Guid))
            {
                code.Add(ReadingCode(p, "packer.ReadGuid()"));
            }
            else if (p.MemberType == typeof(Char))
            {
                code.Add(ReadingCode(p, "packer.ReadChar()"));
            }
            else if (p.MemberType == typeof(Decimal))
            {
                code.Add(ReadingCode(p, "packer.ReadDecimal()"));
            }
            else if (p.MemberType.IsDefined(typeof(GSerializableAttribute), inherit: false))
            {
                var statementRead = $"{ClassNameGenerated(p.MemberType)}.Read(serializer)";
                code.Add(ReadingCode(p, statementRead));
            }
            else if (p.MemberType.Name == "List`1" && p.MemberType.IsGenericType)
            {
                var itemType = p.MemberType.GetGenericArguments()[0];
                var statementRead = $"CollectionPacker.ReadList<{FullClassName(itemType)}>(serializer)";
                code.Add(ReadingCode(p, statementRead));
            }
            else if (p.MemberType.Name == "Nullable`1" && p.MemberType.IsGenericType)
            {
                var valueType = p.MemberType.GetGenericArguments()[0];
                var statementRead = $"CollectionPacker.ReadNullable<{FullClassName(valueType)}>(serializer)";
                code.Add(ReadingCode(p, statementRead));
            }
            else if (p.MemberType.Name == "Dictionary`2" && p.MemberType.IsGenericType)
            {
                var keyType = p.MemberType.GetGenericArguments()[0];
                var valueType = p.MemberType.GetGenericArguments()[1];
                var statementRead = $"CollectionPacker.ReadDict<{FullClassName(keyType)},{FullClassName(valueType)}>(serializer)";
                code.Add(ReadingCode(p, statementRead));
            }
            else if (p.MemberType.IsArray && p.MemberType.HasElementType)
            {
                var elementType = p.MemberType.GetElementType();
                var statementRead = $"CollectionPacker.ReadArray<{FullClassName(elementType)}>(serializer)";
                code.Add(ReadingCode(p, statementRead));
            }
            else if (p.MemberType.IsEnum)
            {
                var statementRead = $"CollectionPacker.ReadEnum<{FullClassName(p.MemberType)}>(serializer)";
                code.Add(ReadingCode(p, statementRead));
            }
            else
            {
                throw new NotSupportedException($"{p.MemberType} of {p.MemberName} is not a supported type");
            }
            return code;
        }

        private static List<PropertyFieldInfo> FindProperties(Type type)
        {
            var result = new List<PropertyFieldInfo>();
            foreach(var a in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!a.IsDefined(typeof(IgnoredAttribute), inherit: false) && a.CanWrite)
                {
                    var isOptional = a.IsDefined(typeof(OptionalAttribute), inherit: false);
                    result.Add(new PropertyFieldInfo 
                    {
                        MemberType = a.PropertyType, 
                        MemberName = a.Name,
                        IsOptional = isOptional
                    });
                }
            }

            foreach (var a in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!a.IsDefined(typeof(IgnoredAttribute), inherit: false))
                {
                    var isOptional = a.IsDefined(typeof(OptionalAttribute), inherit: false);
                    result.Add(new PropertyFieldInfo 
                    { 
                        MemberType = a.FieldType, 
                        MemberName = a.Name,
                        IsOptional = isOptional
                    });
                }
            }
            return result;
        }
    }

    class PropertyFieldInfo
    {
        public Type MemberType;
        public string MemberName;
        public bool IsOptional;
    }
}
