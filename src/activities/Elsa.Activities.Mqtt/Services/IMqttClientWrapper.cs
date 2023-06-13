using Elsa.Activities.Mqtt.Options;
using MQTTnet;
using MQTTnet.Client;
using System;
using System.Threading.Tasks;

namespace Elsa.Activities.Mqtt.Services
{
    public interface IMqttClientWrapper : IDisposable
    {
        IMqttClient Client { get; }
        Options.MqttClientOptions Options { get; }
        Task PublishMessageAsync(string topic, string message);
        Task SetMessageHandlerAsync(Func<MqttApplicationMessage, Task> handler);
    }
}
