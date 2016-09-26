﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Hosting.Self;

namespace BrightstarDB.Server.Runner
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            try {
#if __MonoCS__
            if (AppDomain.CurrentDomain.FriendlyName == "BrightstarDB"){
                RunAsService();
            } else {
                RunAsConsoleApp(args);
            }
#else
            if (Environment.UserInteractive)
            {
                RunAsConsoleApp(args);
            }
            else
            {
                RunAsService();
            }
#endif
            } catch (Exception ex) {
              Console.WriteLine(ex);
            }
        }

        private static void RunAsService()
        {
// Running as a service
            var servicesToRun = new ServiceBase[]
                {
                    new Service()
                };
            ServiceBase.Run(servicesToRun);
        }

        private static void RunAsConsoleApp(string[] args)
        {
// Running from the command line
            WriteWelcomeHeader();
#if DEBUG
            Logging.EnableConsoleOutput(true);
#else
            Logging.EnableConsoleOutput(false);
#endif

            var serviceArgs = new ServiceArgs();
            if (CommandLine.Parser.ParseArgumentsWithUsage(args, serviceArgs))
            {
                try
                {
					Console.WriteLine("BrightstarDB Service is starting...");
                    var bootstrapper = ServiceBootstrap.GetBootstrapper(serviceArgs);
                    var baseUris = serviceArgs.BaseUris.Select(x => x.EndsWith("/") ? new Uri(x) : new Uri(x + "/")).ToArray();
                    var nancyHost = new NancyHost(bootstrapper, new HostConfiguration {AllowChunkedEncoding = false}, baseUris);
                    var nancyEnvironment = bootstrapper.GetEnvironment();
                    nancyEnvironment.Tracing(displayErrorTraces: serviceArgs.ShowErrorTraces, enabled:true);
                    nancyHost.Start();
					Console.WriteLine("BrightstarDB Service is running. Hit Enter to stop the service.");
                    Console.ReadLine();
					Console.WriteLine("Stopping BrightstarDB Service...");
                    nancyHost.Stop();
					Console.WriteLine("BrightstarDB Service stopped.");
                }
                catch (BootstrapperException ex)
                {
                    Console.WriteLine(ex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unhandled exception in server: {0}", ex.Message);
                }
            }
        }

        private static void WriteWelcomeHeader()
        {
            var fvi = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            var assemVer = fvi.FileMajorPart + "." + fvi.FileMinorPart + "." + fvi.FileBuildPart;

            Console.WriteLine("BrightstarDB REST Server {0}.", assemVer);
            Console.WriteLine(fvi.LegalCopyright);
            Console.WriteLine("-------------------------------------------------------------------------------");
            Console.WriteLine();
        }
    }
}
