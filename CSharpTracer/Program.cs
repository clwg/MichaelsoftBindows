using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.Diagnostics.Tracing.Parsers;
using CsharpTracer.Helpers;
using CsharpTracer.Handlers;

namespace CsharpTracer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting System Monitor...");

            InternalDnsCache.AddReverseIp("127.0.0.1", "localhost");

            // Start the DNS monitoring session
            using var dnssession = new TraceEventSession("DnsMonitorSession");

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

            // Start the standard trace session
            using var session = new TraceEventSession("FullMonitorSession");

            session.EnableKernelProvider(
                KernelTraceEventParser.Keywords.Registry |
                KernelTraceEventParser.Keywords.ImageLoad |
                KernelTraceEventParser.Keywords.Thread |
                KernelTraceEventParser.Keywords.NetworkTCPIP |
                KernelTraceEventParser.Keywords.Process
            );

            session.Source.Kernel.TcpIpConnect += data => Handlers.NetworkEvent.HandleNetworkEvent("NetworkTcpIp", data);

            // Uncomment and add handlers as needed
            session.Source.Kernel.RegistryCreate += data =>  RegistryEvent.HandleRegistryEvent("RegistryCreate", data);
            session.Source.Kernel.RegistryDelete += data => RegistryEvent.HandleRegistryEvent("RegistryDelete", data);
            session.Source.Kernel.RegistrySetValue += data => RegistryEvent.HandleRegistryEvent("RegistrySetValue", data);
            session.Source.Kernel.RegistrySetInformation += data => RegistryEvent.HandleRegistryEvent("RegistrySetInformation", data);
            session.Source.Kernel.ProcessStart += data => ProcessEvent.HandleProcessEvent("ProcessStart", data);
            session.Source.Kernel.ProcessStop += data => ProcessEvent.HandleProcessEvent("ProcessStop", data);
            session.Source.Kernel.ThreadStart += data => ThreadEvent.HandleThreadEvent("ThreadStart", data);
            session.Source.Kernel.ImageLoad += data => ImageEvent.HandleImageEvent("ImageLoad", data);
            session.Source.Kernel.ImageUnload += data => ImageEvent.HandleImageEvent("ImageUnload", data);
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