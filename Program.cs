using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// Azure Management dependencies
using Microsoft.Rest.Azure.Authentication;
using Microsoft.Rest.Azure.OData;

// These examples correspond to the Monitor .Net SDK versions 0.16.0-preview and 0.16.1-preview
// Those versions include the single-dimensional metrics API.
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Monitor.Fluent;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.Monitor.Fluent.Models;
using Newtonsoft.Json;

namespace AzureMonitorCSharpExamples
{
    public class Program
    {

        private struct CPUMetric {
            public string vm { get; set; }
            public DateTime time { get; set; }
            public double value { get; set; }
        }

        private static IAzure _azure;
        private static MonitorManagementClient _monitor;

        public static void Main(string[] args)
        {
            var tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
            var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
            var secret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET");
            var subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
            if (new List<string> { tenantId, clientId, secret, subscriptionId }.Any(i => String.IsNullOrEmpty(i)))
            {
                Console.WriteLine("Please provide environment variables for AZURE_TENANT_ID, AZURE_CLIENT_ID, AZURE_CLIENT_SECRET and AZURE_SUBSCRIPTION_ID.");
            }
            else
            {
                Authenticate(tenantId, clientId, secret, subscriptionId);
                // RunMetricDefinitionsSample(readOnlyClient, resourceId).Wait();
                // RunMetricsSample(readOnlyClient, resourceId).Wait();
                GatherMetrics().Wait();
             }
        }

        #region job

        private static async Task<IEnumerable<CPUMetric>> GetVMMetric(IVirtualMachine vm) {

            const string iso8601Format = "yyyy-MM-ddTHH:mm:ssZ";
            string startDate = DateTime.Now.AddDays(-30)
                                    .ToString(iso8601Format);
            string endDate = DateTime.Now
                                    // .ToString("o");
                                    .ToString(iso8601Format);
            string timespan = $"{startDate}/{endDate}";
            var metrics = await _monitor.Metrics.ListAsync(
                resourceUri: vm.Id,
                aggregation: "Average",
                metricnames: "Percentage CPU",
                resultType: ResultType.Data,
                timespan: timespan,
                interval: TimeSpan.FromDays(1),
                cancellationToken: CancellationToken.None);

            return metrics.Value
                        .SelectMany(m => m.Timeseries)
                        .SelectMany(t => t.Data)
                        .Select(i => new CPUMetric {
                            vm = $"{vm.ResourceGroupName}/{vm.Name}",
                            time = i.TimeStamp,
                            value = i.Average ?? 0
                        });
        }

        private static async Task GatherMetrics()
        {
            var results =
                (await Task.WhenAll(
                    (await _azure.VirtualMachines.ListAsync())
                    .Select(vm => GetVMMetric(vm))
                    .ToList()
                ))
                .SelectMany(i => i);
            Console.WriteLine("vm,time,value");
            foreach (var result in results) {
                Console.WriteLine($"{result.vm},{result.time},{result.value}");
            }
        }

        #endregion

        #region Authentication
        private static void Authenticate(string tenantId, string clientId, string secret, string subscriptionId)
        {
            var creds = SdkContext.AzureCredentialsFactory.FromServicePrincipal(
                clientId, secret, tenantId, AzureEnvironment.AzureGlobalCloud
            );
            _azure = Azure
                    .Authenticate(creds)
                    .WithSubscription(subscriptionId);
            _monitor = new MonitorManagementClient(creds);
            _monitor.SubscriptionId = subscriptionId;
        }

        #endregion
    }
}