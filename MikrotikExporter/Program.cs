using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Prometheus;
using Prometheus.Advanced;
using tik4net;
using tik4net.Objects;
using tik4net.Objects.CapsMan;

namespace MikrotikExporter
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            var con = await ConnectionFactory.OpenConnectionAsync(TikConnectionType.ApiSsl_v2, "192.168.0.1", "read",
                "");
            DefaultCollectorRegistry.Instance.RegisterOnDemandCollectors(new MikrotikCollector(con));
            var server = new MetricServer( 1234).Start();
            Console.WriteLine($"Started at {DateTime.Now}");

            ManualResetEvent quitEvent = new ManualResetEvent(false);
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
        private Counter rxBytes;
        private Gauge signal;
        private Counter txBytes;

        public MikrotikCollector(ITikConnection connection)
        {
            this.connection = connection;
        }


        public void RegisterMetrics(ICollectorRegistry registry)
        {
            txBytes = Metrics.WithCustomRegistry(registry).CreateCounter("CAPsMAN_Clients_TxBytes",
                "Bytes transmitted to the CAPsMAN client", "MAC");
            rxBytes = Metrics.WithCustomRegistry(registry).CreateCounter("CAPsMAN_Clients_RxBytes",
                "Bytes received from the CAPsMAN client", "MAC");
            signal = Metrics.WithCustomRegistry(registry)
                .CreateGauge("CAPsMAN_Clients_Signal", "CAPsMAN Client's signal", "MAC");
        }

        public void UpdateMetrics()
        {
            var clients = connection.LoadAll<CapsManRegistrationTable>();
            foreach (var client in clients)
            {
                var match = Regex.Match(client.Bytes, "([0-9]+),([0-9]+)");

                var txBytesClient = txBytes.WithLabels(client.MACAddress);
                txBytesClient.Inc(long.Parse(match.Groups[1].Value) - txBytesClient.Value);

                var rxBytesClient = rxBytes.WithLabels(client.MACAddress);
                rxBytesClient.Inc(long.Parse(match.Groups[2].Value) - rxBytesClient.Value);

                var signalClient = signal.WithLabels(client.MACAddress);
                signalClient.Set(client.Signal);
            }
        }
    }
}