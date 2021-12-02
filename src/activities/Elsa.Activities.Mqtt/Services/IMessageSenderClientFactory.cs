using Elsa.Activities.Mqtt.Options;

namespace Elsa.Activities.Mqtt.Services
{
    public interface IMessageSenderClientFactory
    {
        Task<IMqttClientWrapper> GetSenderAsync(MqttClientOptions configuration, CancellationToken cancellationToken = default);
    }
}