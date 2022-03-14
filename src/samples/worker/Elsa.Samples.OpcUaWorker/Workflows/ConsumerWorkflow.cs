using Elsa.Activities.Console;
using Elsa.Activities.OpcUa;
using Elsa.Builders;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace Elsa.Samples.OpcUaWorker.Workflows
{
    public class ConsumerWorkflow : IWorkflow
    {
        private readonly string _connectionString;
        public readonly Dictionary<string, string> _tags;
        public ConsumerWorkflow(IConfiguration configuration)
        {
            //_connectionString = "opc.tcp://localhost:50000";
            _connectionString = "opc.tcp://192.168.1.7:62541/Quickstarts/ReferenceServer";

            //_connectionString = "opc.tcp://opcuaserver.com:48484";

            _tags = new Dictionary<string, string>();
            //_tags.Add(@"ns=2;s=Arpa.OPClient.Modbus.random", "random");
            _tags.Add(@"ns=2;s=Contact_Arpa", "Arpa");
            //_tags.Add(@"ns=1;s=Countries.US.Queens.Latitude", "latitude");

        }

        public void Build(IWorkflowBuilder builder)
        {
            builder
                .MessageReceived(_connectionString, _tags)
                .WriteLine(context =>
                {
                    
                    return $"Received a weather update saying pippo";
                });
        }
    }
}
