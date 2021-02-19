/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using XPRPC;
using XPRPC.Client;
using XPRPC.Server;
using XPRPC.Service.Logger;

namespace TestRpc
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                if (args[0] == "RunServiceManager")
                {
                    RunServiceManager();
                }
                else if (args[0] == "RunEchoServer")
                {
                    RunEchoServer();
                }                
            }
            else
            {
                TestLocal();
                TestTcp();
            }
        }

        static void TestLocal()
        {
            //Server site code
            var descManager = new ServiceDescriptor
            {
                Name = "service_manager",
                Description = "service manager",
            };
            using var managerRunner = LocalManagerRunner.Instance;
            var config = AccessConfig.FromJson(ManagerConfigJson);
            managerRunner.Config(config);
            managerRunner.Start(descManager, sslCertificate: null);

            var descLogger = new ServiceDescriptor
            {
                Name = "logger",
                Description = "log service",
            };
            using var loggerService = BuilderLogger();
            using var loggerRunner = new LocalServiceRunner<ILogger>(loggerService, descLogger);
            loggerRunner.Start(descManager, clientID: "logger_provider", secretKey: "Dx90et54");

            var echoDescriptor = new ServiceDescriptor
            {
                Name = "echo",
                Description = "demo service",
            };
            
            var echoService = new EchoImpl();
            using var echoRunner = new LocalServiceRunner<IEcho2>(echoService, echoDescriptor);
            echoRunner.Start(descManager, clientID: "echo_provider",secretKey: "F*ooE3");

            //client site code
            using var resolver = new LocalServiceResolver(descLogger, clientID: "logger_client",secretKey: "02384Je5");
            var services = resolver.ServiceManager.ListService();
            foreach (var desc in services)
            {
                Console.WriteLine(desc.ToString());
            }

            var loggerClient = resolver.GetService<ILogger>("logger");
            loggerClient.Debug(tag: "local_test", message: "Hello XPRPC");       

            var echoClient = resolver.GetService<IEcho2>("echo");
            //call sync methods         
            Console.WriteLine(echoClient.SayHello("World!"));
            Console.WriteLine(echoClient.SayHi("XP!"));
            //add event handler           
            echoClient.GreetingEvent += (sender, args) => { Console.WriteLine(args.Greeting); };
            echoService.Greeting("Hello clients!");
            echoClient.Greeting2Event += OnEchoGreeting;
            echoService.Greeting("Hello echo!");
            //callback & async method call
            echoClient.SetCallback(new Callback());
            echoService.GreetingAsync("Hello echo async!").Wait();
            echoClient.SetCallback(null);
            echoService.GreetingAsync("Hello echo async agin!").Wait();
            //remove event handler
            echoClient.Greeting2Event -= OnEchoGreeting;
            echoService.Greeting("Hello echo two!");
            Console.WriteLine(echoClient.SayHelloAsync("World!").Result);
        }

        static string MakeProcessArg(string arg)
        {            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return arg;
            var dll = new Uri(typeof(Program).Assembly.CodeBase).LocalPath;
            return $"\"{dll}\" {arg}";
        }

        static void TestTcp()
        {
            //Start service manager process
            var exe = Process.GetCurrentProcess().MainModule.FileName;
            
            Process managerProcess = new Process();
            managerProcess.StartInfo.FileName = exe;
            managerProcess.StartInfo.Arguments = MakeProcessArg("RunServiceManager");
            managerProcess.Start();
            managerProcess.WaitForExit(milliseconds: 10000);

            //Start echo service process
            Process echoProcess = new Process();
            echoProcess.StartInfo.FileName = exe;
            echoProcess.StartInfo.Arguments = MakeProcessArg("RunEchoServer");
            echoProcess.Start();
            echoProcess.WaitForExit(milliseconds: 5000);

            TestEcho();

            echoProcess.WaitForExit();
            managerProcess.Kill();
        }

        static void TestEcho()
        {
            //client site code
            var descManager = new ServiceDescriptor
            {
                Name = "service_manager",
                Description = "global service manager",
                ServiceHost = "localhost",
                ServicePort = 3324,
                AccessToken = "AnyClient"
            };
            using var resolver = new TcpServiceResolver(descManager, clientID: "logger_client", secretKey: "02384Je5");
            IServiceManager managerProxy = resolver.ServiceManager;
            var services = managerProxy.ListService();
            foreach (var desc in services)
            {
                Console.WriteLine(desc.ToString());
            }

            IEcho2 echoClient;
            while (true) //wait for server ready
            {
                echoClient = resolver.GetService<IEcho2>("echo");
                if (echoClient == null)
                    Thread.Sleep(100);
                else
                    break;
            }
            //sync method call         
            Console.WriteLine(echoClient.SayHello("World!"));
            Console.WriteLine(echoClient.SayHi("XP!"));
            //add event handler           
            echoClient.GreetingEvent += (sender, args) => { Console.WriteLine(args.Greeting); };
            echoClient.Greeting("Hello clients!");
            echoClient.Greeting2Event += OnEchoGreeting;
            echoClient.Greeting("Hello echo!");
            //callback & async method call
            echoClient.SetCallback(new Callback());
            echoClient.GreetingAsync("Hello echo async!").Wait();
            echoClient.SetCallback(null);
            echoClient.GreetingAsync("Hello echo async agin!").Wait();
            //remove event handler
            echoClient.Greeting2Event -= OnEchoGreeting;
            echoClient.Greeting("Hello echo two!");
            Console.WriteLine(echoClient.SayHelloAsync("World!").Result);
            echoClient.Stop();
        }

        private static void OnEchoGreeting(Object sender, EchoEventArgs args)
        {
            Console.WriteLine($"Server says {args.Greeting}");
        }

        static void RunServiceManager()
        {
            var logger = BuilderLogger();
            var descManager = new ServiceDescriptor
            {
                Name = "service_manager",
                Description = "global service_manager",
                ServiceHost = "localhost",
                ServicePort = 3324,  //service manager must specify a port
                AccessToken = "AnyClient"
            };
            using var managerRunner = TcpManagerRunner.Instance;
            managerRunner.Logger = logger;
            var config = AccessConfig.FromJson(ManagerConfigJson);
            managerRunner.Config(config);
            managerRunner.Start(descManager, sslCertificate: null);

            var loggerDescriptor = new ServiceDescriptor
            {
                Name = "logger",
                Description = "log service",
                ServiceHost = "localhost",
                ServicePort = 3325,
            };
            using var loggerRunner = new TcpServiceRunner<ILogger>(
                service: logger,
                descriptor: loggerDescriptor,
                logger: logger,
                sslCertificate: null);
            loggerRunner.Start(descManager, clientID: "logger_provider", secretKey: "Dx90et54");
            while(true)
            {
                Thread.Sleep(1000);
            }
        }

        static void RunEchoServer()
        {
            var echoDescriptor = new ServiceDescriptor
            {
                Name = "echo",
                Description = "demo service",
                ServiceHost = "localhost",
                ServicePort = 0, //the port can be set to any
            };
            var descManager = new ServiceDescriptor
            {
                Name = "service_manager",
                Description = "global service manager",
                ServiceHost = "localhost",
                ServicePort = 3324,
                AccessToken = "AnyClient"
            };

            using var resolver = new TcpServiceResolver(descManager, clientID: "logger_client", secretKey: "02384Je5");
            var remoteLogger = resolver.GetService<ILogger>("logger");
            
            var echoService = new EchoImpl();
            using var echoRunner = new TcpServiceRunner<IEcho2>(
                service: echoService,                  
                descriptor: echoDescriptor, 
                logger: remoteLogger,
                sslCertificate: null);
            echoRunner.Start(descManager, clientID: "echo_provider",secretKey: "F*ooE3");

            echoService.WaitForStop();
            Thread.Sleep(2000); //sleep a while so that the client can release the proxy first
        }

        static ILogger BuilderLogger()
        {
            var builder = new LoggerBuilder
            {
                ConsoleEnabled = false,
                DebugEnabled = true,
                LogFile = "default.log"
            };
            return builder.Build();
        }

        static string ManagerConfigJson = @"{
            ""service_client_access"" : {
                ""logger"":[""any""],
                ""echo"":[""logger_client""]
            },
            ""service_provider_client"": [""logger_provider"", ""echo_provider""],
            ""client_secrect"": {
                ""logger_provider"": ""Dx90et54"",
                ""echo_provider"": ""F*ooE3"",
                ""logger_client"": ""02384Je5"",
            },            
        }";
    }


    [Serializable]
    public class EchoEventArgs: EventArgs
    {
        public string Greeting {get; set;}
    }
    public interface IEcho
    {
        string SayHello(string message);
        void Greeting(string greeting);
        Task<string> SayHelloAsync(string message);
        event EventHandler<EchoEventArgs> GreetingEvent;        
    }

    public interface IEcho2: IEcho
    {
        string SayHi(string message);
        event EventHandler<EchoEventArgs> Greeting2Event;

        Task GreetingAsync(string message);

        void SetCallback(IEchoListener listener);

        void Stop();
    }

    public interface IEchoListener
    {
        void OnGreeting(string message);
    }

    public class EchoImpl : IEcho2
    {
        public event EventHandler<EchoEventArgs> GreetingEvent;
        public event EventHandler<EchoEventArgs> Greeting2Event;
        private IEchoListener _listener;


        public string SayHello(string message)
        {
            return $"Hello {message}";
        }

        public void Greeting(string greeting)
        {
            GreetingEvent?.Invoke(this, new EchoEventArgs{ Greeting = $"1: {greeting}"});            
            Greeting2Event?.Invoke(this, new EchoEventArgs{ Greeting = $"2: {greeting}"});
        }

        public string SayHi(string message)
        {
            return $"Hi {message}";
        }

        public Task<string> SayHelloAsync(string message)
        {
            return Task.FromResult($"hello async {message}");
        }

        public Task GreetingAsync(string message)
        {
            Greeting(message);
            _listener?.OnGreeting($"callback {message}");
            return Task.CompletedTask;
        }
        
        public void SetCallback(IEchoListener listener)
        {
            _listener = listener;
        }


        private bool _stop;
        private object _lock = new object();
        internal void WaitForStop()
        {
            lock(_lock)
            {
                while(!_stop)
                {
                    Monitor.Wait(_lock);
                }
            }
        }

        public void Stop()
        {
            lock(_lock)
            {
                _stop = true;
                Monitor.PulseAll(_lock);
            }
        }
    }

    class Callback : IEchoListener
    {
        public void OnGreeting(string message)
        {
            Console.WriteLine(message);
        }
    }
}
