using Elsa.Activities.Console;
using Elsa.Activities.OpcUa;
using Elsa.Builders;
using Microsoft.Extensions.Configuration;
using NodaTime;
using Elsa.Activities.Temporal;
using System.Collections.Generic;
using System;

namespace Elsa.Samples.OpcUaWorker.Workflows
{
    public class ProducerWorkflow : IWorkflow
    {
        private readonly string _connectionString;
        public readonly Dictionary<string, string> _tags;
        private readonly Random _random;
        public ProducerWorkflow(IConfiguration configuration)
        {
            _random = new Random();
            _connectionString = "opc.tcp://localhost:50000";
            //_connectionString = "opc.tcp://opcuaserver.com:4840";

            //_connectionString = "opc.tcp://opcuaserver.com:48484";

            _tags = new Dictionary<string, string>();
            _tags.Add(@"ns=2;s=st2.Counters.availableMaintenanceCounters", "availableMaintenanceCounters");
            //_tags.Add(@"ns=1;s=Countries.US.Queens.Latitude", "latitude");
        }

        public void Build(IWorkflowBuilder builder)
        {
            builder
                .Timer(Duration.FromSeconds(5))
                .WriteLine("Sending a temperature update with the \"/temperature\" topic.")
                .SendMessage(_connectionString, _tags, GetRandomTemperature());
        }

        private string GetRandomTemperature() => $"{_random.Next(4, 32)} degrees Celsium";
    }
}