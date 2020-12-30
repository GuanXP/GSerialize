/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Text;
using System.Linq;
using System.Runtime.Loader;
using GSerialize;
using System.IO;

namespace XPRPC
{
    class ProxyGenerator
    {
        private readonly Dictionary<string, MethodInfo> _factoryMethods = new Dictionary<string, MethodInfo>();
        private readonly Object _lock = new Object();

        private readonly bool OutputDebug = false;
        internal static ProxyGenerator Instance{ get; } = new ProxyGenerator();

        private ProxyGenerator() {}

        public IProxy CreateProxy(ProxyItem proxyItem)
        {
            lock (_lock)
            {
                var serviceType = proxyItem.InterfaceType;
                if (!_factoryMethods.TryGetValue(serviceType.FullName, out MethodInfo factoryMethod))
                {
                    factoryMethod = GenerateProxyFactory(serviceType);
                    _factoryMethods[serviceType.FullName] = factoryMethod;
                }
                var proxy = factoryMethod.Invoke(null, new object[]{ proxyItem });
                return (IProxy) proxy;    
            }         
        } 

        private MethodInfo GenerateProxyFactory(Type serviceType)
        {
            var codeLine = GenerateClassCode(serviceType);
            var referencedAssemblies = DependencyWalker.GetReferencedAssemblies(
                new Assembly[]{serviceType.Assembly, typeof(ProxyItem).Assembly});            
            var compiledAssembly = Compile(codeLine, $"{serviceType.VisibleClassName()}_proxy.dll", referencedAssemblies);
            var fullClassName = $"XPRPC.Generated.{serviceType.GeneratedProxyClassName()}";
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

        private List<string> GenerateClassCode(Type serviceType)
        {
            if (!serviceType.IsInterface)
            {
                throw new NotSupportedException($"{serviceType.Name} must be an interface");
            }
            var codeLines = new List<string>();
            var className = serviceType.GeneratedProxyClassName();
            var interfaceName = serviceType.VisibleClassName();
            codeLines.Add("using System;");
            codeLines.Add("using System.IO;");
            codeLines.Add("using GSerialize;");
            codeLines.Add("using XPRPC;");
            codeLines.Add("using System.Collections.Generic;");
            codeLines.Add("using System.Text;");
            codeLines.Add("using System.Threading.Tasks;");
            codeLines.Add("namespace XPRPC.Generated {");
            codeLines.Add($"public sealed class {className} : {interfaceName}, IProxy");
            codeLines.Add("{");
            codeLines.Add("readonly ProxyItem _proxyItem;");
            //constructor
            codeLines.Add($"private {className}(ProxyItem proxyItem)");
            codeLines.Add("{");
            codeLines.Add("_proxyItem = proxyItem;");
            codeLines.Add("}");
            //Factory method
            codeLines.Add($"public static {className} New(ProxyItem proxyItem)");
            codeLines.Add("{");
            codeLines.Add($"return new {className}(proxyItem);");
            codeLines.Add("}");

            Int16 methodID = 0;
            foreach(var method in serviceType.DeclaredMethods())
            {
                ++methodID;
                codeLines.AddRange(GenerateMethod(methodID, method));
            }

            codeLines.AddRange(GenerateEventNotify(methodID, serviceType));

            foreach(var eventInfo in serviceType.DeclaredEvents())
            {
                methodID += 2;
                codeLines.AddRange(GenerateEvent(methodID, eventInfo));
            }

            codeLines.AddRange(GenerateDispose(serviceType));

            codeLines.Add("}"); //end of class
            codeLines.Add("}"); //end of namespace
            return codeLines;
        }

        private IEnumerable<string> GenerateEventNotify(short methodID, Type serviceType)
        {
            var codeLines = new List<string>();
            codeLines.Add("public void FireEvent(Int16 eventID, Stream serializedEventArgs)");
            codeLines.Add("{");
            var events = serviceType.DeclaredEvents();
            if (events.Count > 0)
            {
                codeLines.Add("switch(eventID){");
                foreach (var eventInfo in serviceType.DeclaredEvents())
                {
                    methodID += 2;
                    codeLines.Add($"case {methodID}: Fire_{eventInfo.Name}(serializedEventArgs); break;");
                }
                codeLines.Add("}");
            }
            codeLines.Add("}");
            return codeLines;
        }

        private IEnumerable<string> GenerateDispose(Type serviceType)
        {
            var codeLines = new List<string>();
            codeLines.Add("private bool _disposed = false;");
            codeLines.Add("private void Dispose(bool disposing)");
            codeLines.Add("{");
            codeLines.Add("if (disposing && !_disposed)");
            codeLines.Add("{");
            codeLines.Add("_disposed = true;");
            foreach(var eventInfo in serviceType.DeclaredEvents())
            {
                codeLines.Add($"Remove_{eventInfo.Name}();"); //unregister events
            }
            codeLines.Add("}");
            codeLines.Add("}");
            codeLines.Add("public void Dispose()");
            codeLines.Add("{");
            codeLines.Add("Dispose(disposing: true);");
            codeLines.Add("GC.SuppressFinalize(this);");
            codeLines.Add("}");
            return codeLines;
        }

        private IEnumerable<string> GenerateEvent(short methodID, System.Reflection.EventInfo eventInfo)
        {
            var privateEvent = $"_{eventInfo.Name}";

            var codeLines = new List<string>();
            codeLines.Add($"private event EventHandler<{eventInfo.ArgsTypeName()}> {privateEvent};");
            codeLines.Add($"public event EventHandler<{eventInfo.ArgsTypeName()}> {eventInfo.Name}");
            codeLines.Add("{");
            
            codeLines.Add("add{");
            codeLines.Add($"Add_{eventInfo.Name}();");
            codeLines.Add($"{privateEvent} += value;");
            codeLines.Add("}");

            codeLines.Add("remove{");
            codeLines.Add($"Remove_{eventInfo.Name}();");
            codeLines.Add($"{privateEvent} -= value;");            
            codeLines.Add("}");

            codeLines.Add("}");

            codeLines.Add($"private void Add_{eventInfo.Name}()");
            codeLines.Add("{");
            codeLines.Add($"if ({privateEvent} != null) return;");
            if(OutputDebug)
            {
                var msg = $"proxy: add_event {eventInfo.Name} id={methodID}";
                codeLines.Add($"System.Console.WriteLine(\"{msg}\");");
            }
            codeLines.Add("using var stream = new MemoryStream();");
            codeLines.Add($"_proxyItem.SendCallRequest({methodID}, stream.ToArray());");
            codeLines.Add("}");

            codeLines.Add($"private void Remove_{eventInfo.Name}()");
            codeLines.Add("{");
            codeLines.Add($"if ({privateEvent} == null) return;");
            if(OutputDebug)
            {
                var msg = $"proxy: remove_event {eventInfo.Name} id={methodID}";
                codeLines.Add($"System.Console.WriteLine(\"{msg}\");");
            }
            codeLines.Add("try {");
            codeLines.Add("using var stream = new MemoryStream();");
            codeLines.Add($"_proxyItem.SendCallRequest({methodID + 1}, stream.ToArray());");
            codeLines.Add("} catch {}"); //we need catch all exceptions since the server might out of work
            codeLines.Add("}");

            codeLines.Add($"private void Fire_{eventInfo.Name}(Stream dataStream)");       
            codeLines.Add("{");
            if(OutputDebug)
            {
                var msg = $"proxy: on_event {eventInfo.Name} id={methodID}";
                codeLines.Add($"System.Console.WriteLine(\"{msg}\");");
            }
            codeLines.Add("var serializer = new Serializer(dataStream);");
            var argType = eventInfo.EventHandlerType.GenericTypeArguments[0];
            codeLines.Add($"var args = {SerializeStatement.DeserializeObject(argType)}");
            codeLines.Add($"var handler = {privateEvent};");                
            codeLines.Add($"if (handler != null) handler(this, args);");
            codeLines.Add("}");
            
            return codeLines;
        }

        private IEnumerable<string> GenerateMethod(short methodID, System.Reflection.MethodInfo method)
        {
            if (method.IsIndexer()) throw new NotSupportedException("Indexer is not supported, please use setter/getter");
            
            var codeLines = new List<string>();
            if (method.IsSynchronized())
            {
                codeLines.Add($"public {method.ReturnName()} {method.Name}(");
            }
            else
            {
                codeLines.Add($"public async {method.ReturnName()} {method.Name}(");
            }

            var isFirstParam = true;
            foreach(var p in method.GetParameters())
            {
                var paramDeclare = $"{p.ParameterType.VisibleClassName()} {p.Name}";
                if (!isFirstParam)
                {
                    codeLines.Add($", {paramDeclare}");
                } 
                else
                {
                    codeLines.Add(paramDeclare);
                }                
                isFirstParam = false;
            }
            codeLines.Add("){");
            if(OutputDebug)
            {
                var msg = $"proxy: call {method.Name} id={methodID}";
                codeLines.Add($"System.Console.WriteLine(\"{msg}\");");
            }
            codeLines.Add("var stream = new MemoryStream();");
            codeLines.Add("var serializer = new Serializer(stream);");
            foreach(var p in method.GetParameters())
            {
                if (p.ParameterType.IsInterface)
                {
                    codeLines.Add($"if ({p.Name} == null)");
                    codeLines.Add("{");
                    codeLines.Add("serializer.Serialize<Int16>(-1);");
                    codeLines.Add("} else {");
                    codeLines.Add($"var objectID = _proxyItem.CacheService({p.Name});");
                    codeLines.Add("serializer.Serialize<Int16>(objectID);");
                    codeLines.Add("}");
                }
                else
                {
                    codeLines.Add(SerializeStatement.SerializeObject(p.ParameterType, p.Name));
                }
            }
            
            codeLines.Add("stream.Seek(0, SeekOrigin.Begin);");
            if (method.IsSynchronized())
            {
                codeLines.Add($"var response = _proxyItem.SendCallRequest({methodID}, stream.ToArray());");
            }
            else
            {
                codeLines.Add($"var response = await _proxyItem.SendCallRequestAsync({methodID}, stream.ToArray());");
            }

            if (!method.ReturnType.IsVoid() && !method.ReturnType.IsTask())
            {
                codeLines.Add("serializer = new Serializer(response);");
                
                if (method.ReturnType.IsGenericTask())
                {
                    var returnType = method.ReturnType.GetGenericArguments()[0];
                    codeLines.Add($"return {SerializeStatement.DeserializeObject(returnType)}");
                }
                else if (method.ReturnType.IsInterface)
                {
                    codeLines.Add("var returnObjectID = serializer.Deserialize<Int16>();");
                    var nonNull = $"_proxyItem.CacheProxy<{method.ReturnType.VisibleClassName()}>(returnObjectID)";
                    codeLines.Add($"return returnObjectID >= 0 ? {nonNull} : null;");
                }
                else
                {
                    var returnType = method.ReturnType;
                    codeLines.Add($"return {SerializeStatement.DeserializeObject(returnType)}");
                }
            }

            codeLines.Add("}");
            return codeLines;
        }
    }
}