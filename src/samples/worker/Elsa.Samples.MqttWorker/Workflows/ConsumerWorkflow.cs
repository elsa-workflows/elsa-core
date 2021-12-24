using Elsa.Activities.Console;
using Elsa.Activities.Mqtt;
using Elsa.Activities.Temporal;
using Elsa.Builders;
using Microsoft.Extensions.Configuration;
using NodaTime;
using System.Net.Mqtt;

namespace Elsa.Samples.MqttWorker.Workflows
{
    public class ConsumerWorkflow : IWorkflow
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;
        private readonly MqttQualityOfService _qos;
        public ConsumerWorkflow(IConfiguration configuration)
        {
            var section = configuration.GetSection("Mqtt");

            _host = section.GetValue<string>("Host");
            _port = section.GetValue<int>("Port");
            _username = section.GetValue<string>("Username");
            _password = section.GetValue<string>("Password");
            _qos = section.GetValue<MqttQualityOfService>("QualityOfService");
        }

        public void Build(IWorkflowBuilder builder)
        {
            builder
                .Timer(Duration.FromSeconds(5))
                .MessageReceived("/temperature", _host, _port, _username, _password, _qos)
                .WriteLine(context =>
                {
                    var message = context.GetInput<string>();
                    return $"Received a temperature update saying {message}!";
                });
        }
    }
}