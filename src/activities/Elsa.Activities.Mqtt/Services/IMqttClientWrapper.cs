using Elsa.Activities.Mqtt.Options;
using System;
using System.Net.Mqtt;
using System.Threading.Tasks;

namespace Elsa.Activities.Mqtt.Services
{
    public interface IMqttClientWrapper : IDisposable
    {
        IMqttClient Client { get; }
        MqttClientOptions Options { get; }

        Task SubscribeAsync(string topic);
        Task PublishMessageAsync(string topic, string message);
        void SetMessageHandler(Func<MqttApplicationMessage, Task> handler);
    }
}
