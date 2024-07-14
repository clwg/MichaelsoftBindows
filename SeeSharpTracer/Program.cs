using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Parsers;
using System.Text.Json;
using System.Data;
using Microsoft.Extensions.Caching.Memory;
using EtwTracer.Handlers;
using EtwTracer.Helpers;
using static EtwTracer.Helpers.HashGenerator;
using System.Xml.Linq;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using System.Runtime.InteropServices.JavaScript;

namespace EtwTracer
{
    class Program
    {

        static void Main(string[] args)
        {
            Console.WriteLine("Starting System Monitor...");

            // Add localhost to the reverse DNS cache
            ReverseIpCache.AddReverseIp("127.0.0.1", "localhost");

            using var dnssession = new TraceEventSession("DnsMonitorSession");
            using var session = new TraceEventSession("FullMonitorSession");

            // DNS Events - ordering before NetworkTCPIP analysis is important
            dnssession.EnableProvider("Microsoft-Windows-DNS-Client", Microsoft.Diagnostics.Tracing.TraceEventLevel.Informational, 0x00);
            dnssession.Source.Dynamic.All += (data) =>
            {
                if (data.ProviderName == "Microsoft-Windows-DNS-Client")
                    Handlers.DnsEvent.HandleDnsEvent("DnsEvent", data);
            };

            Task dnsTask = Task.Run(() =>
            {
                dnssession.Source.Process();
                Console.WriteLine("DNS Session Started");
            });

            session.EnableKernelProvider(
                KernelTraceEventParser.Keywords.Registry |
                KernelTraceEventParser.Keywords.ImageLoad |
                KernelTraceEventParser.Keywords.Thread |
                KernelTraceEventParser.Keywords.NetworkTCPIP |
                KernelTraceEventParser.Keywords.Process
            );

            session.Source.Kernel.TcpIpConnect += data => Handlers.NetworkEvent.HandleNetworkEvent("NetworkTcpIp", data);

            // Uncomment and add handlers as needed
            
            session.Source.Kernel.RegistryCreate += data => Handlers.RegistryEvent.HandleRegistryEvent("RegistryCreate", data);
            session.Source.Kernel.RegistryDelete += data => Handlers.RegistryEvent.HandleRegistryEvent("RegistryDelete", data);
            session.Source.Kernel.RegistrySetValue += data => Handlers.RegistryEvent.HandleRegistryEvent("RegistrySetValue", data);
            session.Source.Kernel.RegistrySetInformation += data => Handlers.RegistryEvent.HandleRegistryEvent("RegistrySetInformation", data);
            session.Source.Kernel.ProcessStart += data => Handlers.ProcessEvent.HandleProcessEvent("ProcessStart", data);
            session.Source.Kernel.ProcessStop += data => Handlers.ProcessEvent.HandleProcessEvent("ProcessStop", data);
            session.Source.Kernel.ThreadStart += data => Handlers.ThreadEvent.HandleThreadEvent("ThreadStart", data);
            session.Source.Kernel.ImageLoad += data => Handlers.ImageEvent.HandleImageEvent("ImageLoad", data);
            session.Source.Kernel.ImageUnload += data => Handlers.ImageEvent.HandleImageEvent("ImageUnload", data);
            /* 
            */

            Task sessionTask = Task.Run(() =>
            {
                session.Source.Process();
                Console.WriteLine("Full Session Started");
            });

            Task.WaitAll(dnsTask, sessionTask);
        }
    }
}