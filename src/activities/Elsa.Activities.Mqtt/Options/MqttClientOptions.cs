using MQTTnet.Client;
using MQTTnet.Protocol;
using System;

namespace Elsa.Activities.Mqtt.Options
{
    public class MqttClientOptions
    {
        public string Topic { get; }
        public string Host { get; }
        public int Port { get; }
        public string Username { get; }
        public string Password { get; }
        public MqttQualityOfServiceLevel QualityOfService { get; }

        public MqttClientOptions(string topic, string host, int port, string username, string password, MqttQualityOfServiceLevel qos)
        {
            Topic = topic;
            Host = host;
            Port = port;
            Username = username;
            Password = password;
            QualityOfService = qos;
        }

        public MQTTnet.Client.MqttClientOptions GenerateMqttClientOptions()
        {
            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(Host, Port)
                .WithKeepAlivePeriod(new TimeSpan(0,0,30))
                .WithTimeout(new TimeSpan(0,0,30))
                .WithClientId($"Elsa{Guid.NewGuid():N}");

            if (!string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password))
            {
                mqttClientOptions.WithCredentials(Username, Password);
            }

            return mqttClientOptions.Build();
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Topic, Host, Port, Username, Password);
        }
    }
}
