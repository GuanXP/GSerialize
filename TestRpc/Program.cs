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
            //TestLocal();
            TestTcp();
        }

        static void TestLocal()
        {
            //Server site
            var descManager = new ServiceDescriptor
            {
                Name = "service_manager",
                Description = "manager of services",
            };
            using var managerRunner = LocalManagerRunner.Instance;
            var config = AccessConfig.FromJson(ManagerConfigJson);
            managerRunner.Config(config);
            managerRunner.Start(descManager, sslCertificate: null);

            var descLogger = new ServiceDescriptor
            {
                Name = "logger",
                Description = "logging service",
            };
            using var loggerRunner = new LocalServiceRunner<ILogger>(BuilderLogger(), descLogger, true);
            loggerRunner.Start(descManager, clientID: "logger_provider", secretKey: "Dx90et54");

            var echoDescriptor = new ServiceDescriptor
            {
                Name = "echo",
                Description = "demo service",
            };
            var echoService = new EchoImpl();
            using var echoRunner = new LocalServiceRunner<IEcho2>(echoService, echoDescriptor, true);
            echoRunner.Start(descManager, clientID: "echo_provider",secretKey: "F*ooE3");

            //client site
            using var resolver = new LocalServiceResolver(descLogger, clientID: "logger_client",secretKey: "02384Je5");
            var services = resolver.ServiceManager.ListService();
            foreach (var desc in services)
            {
                Console.WriteLine(desc.ToString());
            }

            using var loggerClient = resolver.GetService<ILogger>("logger");
            loggerClient.Debug(tag: "local_test", message: "Hello XPRPC");       

            using var echoClient = resolver.GetService<IEcho2>("echo");
            Console.WriteLine(echoClient.SayHello("World!"));
            echoClient.GreetingEvent += (sender, args) => {Console.WriteLine(args.Greeting);};
            echoService.Greeting("Hello clients!");
            echoClient.Greeting2Event += OnEchoGreeting;
            echoService.Greeting("Hello echo!");
            echoClient.Greeting2Event -= OnEchoGreeting;
            echoService.Greeting("Hello echo two!");     
        }

        static void TestTcp()
        {
            //server site

            // deploy ServiceManager
            var logger = BuilderLogger();
            var descManager = new ServiceDescriptor
            {
                Name = "service_manager",
                Description = "manager of services",
                ServiceHost = "localhost",
                ServicePort = 3324,
                AccessToken = "AnyClient"
            };
            using var managerRunner = TcpManagerRunner.Instance;
            managerRunner.Logger = logger;
            var config = AccessConfig.FromJson(ManagerConfigJson);
            managerRunner.Config(config);
            managerRunner.Start(descManager, sslCertificate: null);

            // deploy a service
            var loggerDescriptor = new ServiceDescriptor
            {
                Name = "logger",
                Description = "logging service",
                ServiceHost = "localhost",
                ServicePort = 3325,
            };
            using var loggerRunner = new TcpServiceRunner<ILogger>(
                service: logger,                 
                descriptor: loggerDescriptor, 
                holdService: false,
                logger: logger, 
                sslCertificate: null);
            loggerRunner.Start(descManager, clientID: "logger_provider",secretKey: "Dx90et54");

            // deploy another service
            var echoDescriptor = new ServiceDescriptor
            {
                Name = "echo",
                Description = "demo service",
                //ServiceHost = "localhost",  //if no ServiceHost, the default IP will be used
                ServicePort = 0, //can be any port
            };
            var echoService = new EchoImpl();
            using var echoRunner = new TcpServiceRunner<IEcho2>(
                service: echoService,                  
                descriptor: echoDescriptor, 
                holdService: true,
                logger: logger,
                sslCertificate: null);
            echoRunner.Start(descManager, clientID: "echo_provider",secretKey: "F*ooE3");            

            //client site

            //Construct an resolver
            using var resolver = new TcpServiceResolver(descManager, clientID: "logger_client", secretKey: "02384Je5");
            IServiceManager managerProxy = resolver.ServiceManager;
            var services = managerProxy.ListService();
            foreach (var desc in services)
            {
                Console.WriteLine(desc.ToString());
            }

            // get a service proxy
            var remoteLogger = resolver.GetService<ILogger>("logger");
            remoteLogger.Info(tag: "Tcp Test", message: "remote log message.");

            // get another service proxy
            var echoClient = resolver.GetService<IEcho2>("echo");   
            //call sync methods of the proxy  
            Console.WriteLine(echoClient.SayHello("World!"));
            Console.WriteLine(echoClient.SayHi("XP!")); 
            //events call       
            echoClient.GreetingEvent += (sender, args) => {Console.WriteLine(args.Greeting);};
            echoService.Greeting("Hello clients!");
            echoClient.Greeting2Event += OnEchoGreeting;
            echoService.Greeting("Hello echo!");
            //bi-direction call
            echoClient.SetCallback(new Callback());
            echoService.GreetingAsync("Hello echo async!").Wait();
            echoClient.SetCallback(null);
            echoService.GreetingAsync("Hello echo async agin!").Wait();
            //events
            echoClient.Greeting2Event -= OnEchoGreeting;
            echoService.Greeting("Hello echo two!");            
            Console.WriteLine(echoClient.SayHelloAsync("World!").Result);
        }

        private static void OnEchoGreeting(Object sender, EchoEventArgs args)
        {
            Console.WriteLine($"Server says {args.Greeting}");
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
    public interface IEcho: IDisposable
    {
        string SayHello(string message);
        Task<string> SayHelloAsync(string message);
        event EventHandler<EchoEventArgs> GreetingEvent;        
    }

    public interface IEcho2: IEcho
    {
        string SayHi(string message);
        event EventHandler<EchoEventArgs> Greeting2Event;

        Task GreetingAsync(string message);

        void SetCallback(IEchoListener listener);
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

        public void Dispose()
        {            
        }

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
    }

    class Callback : IEchoListener
    {
        public void OnGreeting(string message)
        {
            Console.WriteLine(message);
        }
    }
}
