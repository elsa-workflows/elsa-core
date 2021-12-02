using Elsa.Activities.Mqtt.Options;
using System.Net.Mqtt;

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
