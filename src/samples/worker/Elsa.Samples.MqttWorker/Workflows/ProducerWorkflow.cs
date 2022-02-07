using Elsa.Activities.Console;
using Elsa.Activities.Temporal;
using Elsa.Builders;
using Microsoft.Extensions.Configuration;
using NodaTime;
using System;
using System.Net.Mqtt;
using Elsa.Activities.Mqtt.Activities.SendMqttMessage;

namespace Elsa.Samples.MqttWorker.Workflows
{
    public class ProducerWorkflow : IWorkflow
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;
        private readonly MqttQualityOfService _qos;
        private readonly Random _random;
        public ProducerWorkflow(IConfiguration configuration)
        {
            var section = configuration.GetSection("Mqtt");

            _host = section.GetValue<string>("Host");
            _port = section.GetValue<int>("Port");
            _username = section.GetValue<string>("Username");
            _password = section.GetValue<string>("Password");
            _qos = section.GetValue<MqttQualityOfService>("QualityOfService");
            _random = new();
        }

        public void Build(IWorkflowBuilder builder)
        {
            builder
                .Timer(Duration.FromSeconds(5))
                .WriteLine("Sending a temperature update with the \"/temperature\" topic.")
                .SendMessage("/temperature", _host, _port, _username, _password, _qos, GetRandomTemperature);
        }

        private string GetRandomTemperature() => $"{_random.Next(4, 32)} degrees Celsium";
    }
}