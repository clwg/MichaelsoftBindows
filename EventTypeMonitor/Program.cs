using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Parsers;
using System.Text.Json;
using System.Linq;
using System.Data;
using Microsoft.Extensions.Caching.Memory;

namespace EtwTracer
{
    class Program
    {
        // Applies throttling logic to prevent excessive logging
        private static readonly MemoryCache _processCacheBuffer = new MemoryCache(new MemoryCacheOptions()
        {
            ExpirationScanFrequency = TimeSpan.FromSeconds(600)
        });

        static void Main(string[] args)
        {
            Console.WriteLine("Starting process injection monitor...");

            using var session = new TraceEventSession("MonitorSession");

            session.EnableKernelProvider(
                KernelTraceEventParser.Keywords.Process |
                KernelTraceEventParser.Keywords.Thread |
                KernelTraceEventParser.Keywords.VirtualAlloc |
                KernelTraceEventParser.Keywords.Memory |
                KernelTraceEventParser.Keywords.ImageLoad |
                KernelTraceEventParser.Keywords.FileIO |
                KernelTraceEventParser.Keywords.Registry |
                KernelTraceEventParser.Keywords.NetworkTCPIP |
                KernelTraceEventParser.Keywords.DiskFileIO |
                KernelTraceEventParser.Keywords.MemoryHardFaults |
                KernelTraceEventParser.Keywords.SystemCall |
                KernelTraceEventParser.Keywords.ContextSwitch |
                KernelTraceEventParser.Keywords.Interrupt |
                KernelTraceEventParser.Keywords.DiskIO |
                KernelTraceEventParser.Keywords.FileIOInit |
                KernelTraceEventParser.Keywords.DeferedProcedureCalls
            );

            var eventCounts = new Dictionary<string, int>();
            var lastUpdate = DateTime.Now;

            session.Source.Kernel.VirtualMemAlloc += data =>
            {
                UpdateEventCount(eventCounts, "VirtualMemAlloc");
                DisplayEventCounts(eventCounts, ref lastUpdate);
            };

            session.Source.Kernel.All += data =>
            {
                UpdateEventCount(eventCounts, data.EventName);
                DisplayEventCounts(eventCounts, ref lastUpdate);
            };

            session.Source.Process();
        }

        private static void UpdateEventCount(Dictionary<string, int> eventCounts, string eventName)
        {
            if (eventCounts.ContainsKey(eventName))
            {
                eventCounts[eventName]++;
            }
            else
            {
                eventCounts[eventName] = 1;
            }
        }

        private static void DisplayEventCounts(Dictionary<string, int> eventCounts, ref DateTime lastUpdate)
        {
            // Update display every 1 second
            if ((DateTime.Now - lastUpdate).TotalSeconds >= 1)
            {
                Console.Clear();
                Console.WriteLine("Event Counts:");
                foreach (var eventCount in eventCounts.OrderBy(ec => ec.Key))
                {
                    Console.WriteLine($"{eventCount.Key}: {eventCount.Value}");
                }
                lastUpdate = DateTime.Now;
            }
        }
    }
}
