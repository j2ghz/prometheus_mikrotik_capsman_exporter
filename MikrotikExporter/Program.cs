using System;
using System.Text.RegularExpressions;
using Prometheus;
using Prometheus.Advanced;
using tik4net;
using tik4net.Objects;

namespace MikrotikExporter
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            var con = await tik4net.ConnectionFactory.OpenConnectionAsync(TikConnectionType.Api_v2, "192.168.0.1", "read", "");
            DefaultCollectorRegistry.Instance.RegisterOnDemandCollectors(new MikrotikCollector(con));
            var server = new Prometheus.MetricServer("127.0.0.1",1234).Start();
            Console.ReadKey();
            await server.StopAsync();
        }
    }

    class MikrotikCollector : IOnDemandCollector
    {
        private readonly ITikConnection connection;
        private Counter txBytes;
        private Counter rxBytes;
        private Gauge signal;

        public MikrotikCollector(ITikConnection connection)
        {
            this.connection = connection;
        }


        public void RegisterMetrics(ICollectorRegistry registry)
        {
             txBytes = Metrics.WithCustomRegistry(registry).CreateCounter("CAPsMAN_Clients_TxBytes", "Bytes transmitted to the CAPsMAN client", "MAC");
             rxBytes = Metrics.WithCustomRegistry(registry).CreateCounter("CAPsMAN_Clients_RxBytes", "Bytes received from the CAPsMAN client", "MAC");
             signal = Metrics.WithCustomRegistry(registry).CreateGauge("CAPsMAN_Clients_Signal", "CAPsMAN Client's signal", "MAC");

        }

        public void UpdateMetrics()
        {
           
            var clients = connection.LoadAll<tik4net.Objects.CapsMan.CapsManRegistrationTable>();
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
