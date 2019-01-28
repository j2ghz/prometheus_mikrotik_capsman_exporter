using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Prometheus;
using Prometheus.Advanced;
using tik4net;
using tik4net.Objects;
using tik4net.Objects.CapsMan;
using tik4net.Objects.Interface;
using tik4net.Objects.Ip.DhcpServer;

namespace MikrotikExporter
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            Trace.Listeners.Add(new TextWriterTraceListener(Console.Error));
            var con = await ConnectionFactory.OpenConnectionAsync(TikConnectionType.ApiSsl_v2, "192.168.0.1", "read",
                "");
            DefaultCollectorRegistry.Instance.RegisterOnDemandCollectors(new MikrotikCollector(con));
            var server = new MetricServer(1234).Start();
            Console.WriteLine($"Started at {DateTime.Now}");

            var quitEvent = new ManualResetEvent(false);
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                Console.WriteLine("Stopping...");
                quitEvent.Set();
                eventArgs.Cancel = true;
            };

            quitEvent.WaitOne();

            server.Stop();
        }
    }

    internal class MikrotikCollector : IOnDemandCollector
    {
        private readonly ITikConnection connection;

        private readonly SetCounter rxBytes = new SetCounter("Mikrotik_Client_RxBytes",
            "Bytes received from the CAPsMAN client", new[] {"MAC", "IP", "Hostname"}, true);

        private readonly SetCounter txBytes = new SetCounter("Mikrotik_Client_TxBytes",
            "Bytes transmitted to the CAPsMAN client", new[] {"MAC", "IP", "Hostname"}, true);

        private Gauge signal;

        public MikrotikCollector(ITikConnection connection)
        {
            this.connection = connection;
        }


        public void RegisterMetrics(ICollectorRegistry registry)
        {
            registry.GetOrAdd(txBytes);
            registry.GetOrAdd(rxBytes);
            signal = Metrics.WithCustomRegistry(registry)
                .CreateGauge("CAPsMAN_Clients_Signal", "CAPsMAN Client's signal", "MAC", "IP", "Hostname");
        }

        public void UpdateMetrics()
        {
            var capsManRegistrationTables = connection.LoadAll<CapsManRegistrationTable>();
            var dhcpServerLeases = connection.LoadAll<DhcpServerLease>();
            var interfaces = connection.LoadAll<Interface>();

            foreach (var client in capsManRegistrationTables)
            {
                var dhcpLease = dhcpServerLeases.SingleOrDefault(l => client.MACAddress == l.MacAddress);

                var match = Regex.Match(client.Bytes, "([0-9]+),([0-9]+)");

                txBytes.WithLabels(client.MACAddress, dhcpLease?.Address ?? "", dhcpLease?.HostName ?? "")
                    .Set(long.Parse(match.Groups[1].Value));

                rxBytes.WithLabels(client.MACAddress, dhcpLease?.Address ?? "", dhcpLease?.HostName ?? "")
                    .Set(long.Parse(match.Groups[2].Value));

                var signalClient =
                    signal.WithLabels(client.MACAddress, dhcpLease?.Address ?? "", dhcpLease?.HostName ?? "");
                signalClient.Set(client.Signal);
            }

            foreach (var client in interfaces)
            {
                var dhcpLease = dhcpServerLeases.SingleOrDefault(l => client.MacAddress == l.MacAddress);

                txBytes.WithLabels(client.MacAddress, dhcpLease?.Address ?? "", dhcpLease?.HostName ?? "")
                    .Set(client.TxByte);

                rxBytes.WithLabels(client.MacAddress, dhcpLease?.Address ?? "", dhcpLease?.HostName ?? "")
                    .Set(client.RxByte);
            }
        }
    }
}