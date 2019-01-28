using System;
using System.Collections.Generic;
using System.Text;
using Prometheus.Advanced;
using Prometheus.Advanced.DataContracts;

namespace MikrotikExporter
{
    class SetCounter : Collector<SetCounterChild>
    {
        public SetCounter(string name, string help, string[] labelNames, bool suppressInitialValue) : base(name, help, labelNames, suppressInitialValue)
        {
        }

        protected override MetricType Type => MetricType.COUNTER;
    }

    class SetCounterChild : Child
    {
        private double _value;
        protected override void Populate(Metric metric)
        {
            metric.counter = new Counter {value = _value};
        }
        public void Set(double value)
        {
            this._value = value;
            this._publish = true;
        }
    }
}