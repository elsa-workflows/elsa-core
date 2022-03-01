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
        public string ClientId { get; }

        public MqttClientOptions(string topic, string host, int port, string username, string password, MqttQualityOfService qos, string clientId)
        {
            Topic = topic;
            Host = host;
            Port = port;
            Username = username;
            Password = password;
            QualityOfService = qos;
            ClientId = clientId;
        }

        public MqttClientCredentials GenerateMqttClientCredentials()
        {
            return new MqttClientCredentials(ClientId, Username, Password);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Topic, Host, Port, Username, Password);
        }
    }
}
