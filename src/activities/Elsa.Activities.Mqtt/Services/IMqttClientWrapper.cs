using System;
using System.Net.Mqtt;
using System.Threading.Tasks;
using Elsa.Activities.Mqtt.Options;

namespace Elsa.Activities.Mqtt.Services
{
    public interface IMqttClientWrapper : IDisposable
    {
        IMqttClient Client { get; }
        MqttClientOptions Options { get; }

        Task SubscribeWithHandlerAsync(string topic, Func<MqttApplicationMessage, Task> handler);
        Task PublishMessageAsync(string topic, string message);
    }
}
