using System;
using System.Net.Mqtt;

namespace Elsa.Activities.Mqtt.Options
{
    public class MqttClientOptions
    {
        public string Topic { get; }
        public string Host { get; }
        public int Port { get; }
        public string Username { get; }
        public string Password { get; }
        public MqttQualityOfService QualityOfService { get; }

        public MqttClientOptions(string topic, string host, int port, string username, string password, MqttQualityOfService qos)
        {
            Topic = topic;
            Host = host;
            Port = port;
            Username = username;
            Password = password;
            QualityOfService = qos;
        }

        public MqttClientCredentials GenerateMqttClientCredentials()
        {
            return new MqttClientCredentials($"Elsa{ Guid.NewGuid():N}", Username, Password);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Topic, Host, Port, Username, Password);
        }
    }
}
