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
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Text;
using System.Runtime.Loader;
using GSerialize;

namespace XPRPC
{
    class DispatcherGenerator
    {
        private readonly Dictionary<string, MethodInfo> _dispatcherFactories = new Dictionary<string, MethodInfo>();
        private readonly Object _lock = new Object();

        public static DispatcherGenerator Instance { get; } = new DispatcherGenerator();
        public readonly bool OutputDebug = true;


        public IDispatcher GetDispatcher(ServiceItem serviceItem)
        {
            return (IDispatcher)GetCompiledFactoryMethod(serviceItem.InterfaceType).Invoke(
                null, 
                new object[]{serviceItem});
        }

        public MethodInfo CompileDispatcher<TService>()
        {
            return GetCompiledFactoryMethod(typeof(TService));
        }

        private MethodInfo GetCompiledFactoryMethod(Type serviceType)
        {
            lock (_lock)
            {
                if (!_dispatcherFactories.TryGetValue(serviceType.FullName, out MethodInfo factoryMethod))
                {
                    factoryMethod = GenerateDispatcherFactory(serviceType);
                    _dispatcherFactories[serviceType.FullName] = factoryMethod;
                }
                return factoryMethod;    
            }
        }

        private MethodInfo GenerateDispatcherFactory(Type serviceType)
        {
            var codeLine = GenerateClassForService(serviceType);
            var referencedAssemblies = DependencyWalker.GetReferencedAssemblies(
                new Assembly[]{serviceType.Assembly, typeof(IDispatcher).Assembly});            
            var compiledAssembly = Compile(codeLine, $"{serviceType.CompilableClassName()}_stub.dll", referencedAssemblies);
            var fullClassName = $"XPRPC.Generated.{serviceType.GeneratedDispatcherClassName()}";
            var classType = compiledAssembly.GetType(fullClassName);            
            var factoryMethod = classType.GetMethod("New");
            System.Diagnostics.Debug.Assert(factoryMethod.IsStatic);
            return factoryMethod;
        }

        private Assembly Compile(List<string> codeLines, string assemblyName, List<Assembly> referencedAssemblies)
        {
            var codeBuilder = new StringBuilder();
            foreach(var line in codeLines)
            {
                codeBuilder.AppendLine(line);
            }
            var sourceCode = codeBuilder.ToString();
            var tree = SyntaxFactory.ParseSyntaxTree(sourceCode);

            var references = new List<MetadataReference>();
            foreach(var a in referencedAssemblies)
            {
                references.Add(MetadataReference.CreateFromFile(a.Location));
            }

            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var compilation = CSharpCompilation.Create(assemblyName)
                .WithOptions(options)
                .AddReferences(references)
                .AddSyntaxTrees(tree);
            using var mem = new MemoryStream();
            var result = compilation.Emit(mem);
            if (!result.Success)
            {
                throw new NotSupportedException(result.Diagnostics[0].ToString());
            }
            mem.Seek(0, SeekOrigin.Begin);
            return AssemblyLoadContext.Default.LoadFromStream(mem);
        }

        private List<string> GenerateClassForService(Type serviceType)
        {
            var codeLines = new List<string>();
            var className = serviceType.GeneratedDispatcherClassName();
            codeLines.Add("using System;");
            codeLines.Add("using System.Collections.Generic;");
            codeLines.Add("using System.Text;");
            codeLines.Add("using System.IO;");
            codeLines.Add("using GSerialize;");
            codeLines.Add("using XPRPC.Server;");
            codeLines.Add("using System.Threading.Tasks;");
            codeLines.Add("namespace XPRPC.Generated {");
            codeLines.Add($"public sealed class {className} : IDispatcher");
            codeLines.Add("{");
            codeLines.Add("readonly ServiceItem _serviceItem;");
            codeLines.Add($"readonly {serviceType.CompilableClassName()} _service;");
            codeLines.Add($"private {className}(ServiceItem serviceItem)");
            codeLines.Add("{");
            codeLines.Add("_serviceItem = serviceItem;");
            codeLines.Add($"_service = ({serviceType.CompilableClassName()})serviceItem.Service;");
            codeLines.Add("}");

            codeLines.Add($"public static {className} New(ServiceItem serviceItem)");
            codeLines.Add("{");
            codeLines.Add($"return new {className}(serviceItem);");
            codeLines.Add("}");

            var methodsDict = new Dictionary<Int16, string>();
            Int16 methodID = 0;
            foreach(var method in serviceType.DeclaredMethods())
            {
                ++methodID;
                methodsDict[methodID] = GeneratedMethodCallName(method, methodID);
                codeLines.AddRange(GenerateMethodCall(method, methodID));
            }

            foreach(var eventInfo in serviceType.DeclaredEvents())
            {
                methodID += 2;
                methodsDict[methodID] = eventInfo.AddingMethodName();
                methodsDict[(Int16)(methodID + 1)] = eventInfo.RemovingMethodName();
                codeLines.AddRange(GenerateEventCall(methodID, eventInfo));
            }

            codeLines.AddRange(GenerateDispatchMethod(methodsDict));
            codeLines.AddRange(GenerateDispose(serviceType));
            codeLines.Add("}");
            codeLines.Add("}");
            return codeLines;
        }

        private IEnumerable<string> GenerateDispatchMethod(Dictionary<Int16, string> methodsDict)
        {
            var codeLines = new List<string>();
            codeLines.Add(" public async Task DispatchMethodCall(short methodID, Stream dataStream, MemoryStream resultStream)");
            codeLines.Add("{");

            codeLines.Add("switch(methodID)");
            codeLines.Add("{");
            foreach(var item in methodsDict)
            {
                codeLines.Add($"case {item.Key}:");
                codeLines.Add($"await {item.Value}(dataStream, resultStream);");
                codeLines.Add("break;");
            }
            codeLines.Add("}");

            codeLines.Add("}");
            return codeLines;
        }

        private string GeneratedMethodCallName(MethodInfo method, Int16 methodID)
        {
            return $"{method.Name}_{methodID}_async";
        }

        private IEnumerable<string> GenerateMethodCall(MethodInfo method, Int16 methodID)
        {
            if (method.IsIndexer()) throw new NotSupportedException("Indexer unsupported, use setter/getter instead");
            var codeLines = new List<string>();
            //the method signature
            if (method.IsSynchronized())
            {
                codeLines.Add($"private Task {GeneratedMethodCallName(method, methodID)}(Stream dataStream, MemoryStream resultStream)");
            }
            else
            {
                codeLines.Add($"private async Task {GeneratedMethodCallName(method, methodID)}(Stream dataStream, MemoryStream resultStream)");
            }
            codeLines.Add("{");
            if(OutputDebug)
            {
                var msg = $"stub: call {method.Name} id={methodID}";
                codeLines.Add($"System.Console.WriteLine(\"{msg}\");");
            }

            //deserialize the parameters
            codeLines.Add("var serializer = new Serializer(dataStream);");
            foreach(var p in method.GetParameters())
            {
                if (p.ParameterType.IsInterface)
                {
                    var id_name = $"{p.Name}_objectID";
                    codeLines.Add($"var {id_name} = serializer.Deserialize<Int16>();");
                    var nonNull = $"_serviceItem.CacheProxy<{p.ParameterType.CompilableClassName()}>({id_name})";
                    codeLines.Add($"{p.ParameterType.CompilableClassName()} {p.Name} = {id_name} < 0 ? null : {nonNull} ;");
                }
                else
                {
                    codeLines.Add($"var {p.Name} = {SerializeStatement.DeserializeObject(p.ParameterType)}");
                }
            }

            //call the service object
            if (method.ReturnType.IsVoid())
            {
                codeLines.Add($"_service.{method.Name}(");
            }
            else if(method.ReturnType.IsTask())
            {
                codeLines.Add($"await _service.{method.Name}(");
            }
            else if(method.ReturnType.IsGenericTask())
            {
                codeLines.Add($"var result = await _service.{method.Name}(");
            }
            else
            {
                codeLines.Add($"var result = _service.{method.Name}(");
            }            

            //pass the parameters
            var isFirstParam = true;
            foreach(var p in method.GetParameters())
            {
                if (isFirstParam)
                {
                    isFirstParam = false;
                }
                else
                {
                    codeLines.Add(", ");
                }
                codeLines.Add($"{p.Name}:{p.Name}");
            }
            codeLines.Add(");");

            //serialize the returns to result stream
            if (!method.ReturnType.IsVoid() && !method.ReturnType.IsTask())
            {
                codeLines.Add("serializer = new Serializer(resultStream);");
                var resultType = method.ReturnType;
                if (method.ReturnType.IsInterface)
                {
                    codeLines.Add("if (result == null) {");
                    codeLines.Add("serializer.Serialize<Int16>(-1);");
                    codeLines.Add("} else {");
                    codeLines.Add($"var resultObjectID = _serviceItem.CacheService<{method.ReturnType.CompilableClassName()}>(result);");
                    codeLines.Add("serializer.Serialize<Int16>(resultObjectID);");
                    codeLines.Add("}");
                }
                else
                {
                    if (method.ReturnType.IsGenericTask())
                    {
                        resultType = resultType.GetGenericArguments()[0];
                    }
                    codeLines.Add(SerializeStatement.SerializeObject(resultType, "result"));
                }
            }            
            if (method.IsSynchronized())
            {
                codeLines.Add("return Task.CompletedTask;");
            }            
            codeLines.Add("}");
            return codeLines;
        }

        private IEnumerable<string> GenerateEventCall(Int16 methodID, EventInfo eventInfo)
        {
            var codeLines = new List<string>();
            var registered = eventInfo.RegisteredFieldName();
            codeLines.Add($"bool {registered} = false;");

            codeLines.Add($"private Task {eventInfo.AddingMethodName()}(Stream dataStream, MemoryStream resultStream)");
            codeLines.Add("{");
            if(OutputDebug)
            {
                var msg = $"stub: add_event {eventInfo.Name} id={methodID}";
                codeLines.Add($"System.Console.WriteLine(\"{msg}\");");
            }
            codeLines.Add($"if (!{registered})");
            codeLines.Add("{");
            codeLines.Add($"{registered} = true;");
            codeLines.Add($"_service.{eventInfo.Name} += {eventInfo.HandlingMethodName()};");
            codeLines.Add("}");
            codeLines.Add("return Task.CompletedTask;");
            codeLines.Add("}");

            codeLines.Add($"private Task {eventInfo.RemovingMethodName()}(Stream dataStream, MemoryStream resultStream)");
            codeLines.Add("{");
            if(OutputDebug)
            {
                var msg = $"stub: remove_event {eventInfo.Name} id={methodID}";
                codeLines.Add($"System.Console.WriteLine(\"{msg}\");");
            }
            codeLines.Add($"if ({registered})");
            codeLines.Add("{");
            codeLines.Add($"{registered} = false;");
            codeLines.Add($"_service.{eventInfo.Name} -= {eventInfo.HandlingMethodName()};");
            codeLines.Add("}");
            codeLines.Add("return Task.CompletedTask;");
            codeLines.Add("}");

            codeLines.Add($"private void {eventInfo.HandlingMethodName()}(Object sender, {eventInfo.ArgsTypeName()} args)");
            codeLines.Add("{");
            codeLines.Add($"_serviceItem.SendEvent({methodID}, args);");
            codeLines.Add("}");

            return codeLines;
        }

        private IEnumerable<string> GenerateDispose(Type serviceType)
        {
            var codeLines = new List<string>();
            
            codeLines.Add("private bool _disposed = false;");

            codeLines.Add("public void Dispose()");
            codeLines.Add("{");
            codeLines.Add("if (!_disposed)");
            codeLines.Add("{");
            codeLines.Add("_disposed = true;");
            if(OutputDebug)
            {
                var msg = $"stub: disposing {serviceType.Name}";
                codeLines.Add($"System.Console.WriteLine(\"{msg}\");");
            }

            //remove event handlers
            foreach (var eventInfo in serviceType.DeclaredEvents())
            {
                var registered = eventInfo.RegisteredFieldName();
                codeLines.Add($"if ({registered})");
                codeLines.Add("{");
                codeLines.Add($"{registered} = false;");
                codeLines.Add($"_service.{eventInfo.Name} -= {eventInfo.HandlingMethodName()};");
                codeLines.Add("}");
            }
            codeLines.Add("}");
            codeLines.Add("}");
            return codeLines;
        }
    }
}