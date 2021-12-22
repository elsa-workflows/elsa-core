using Elsa.Activities.Mqtt.Options;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.Mqtt.Services
{
    public interface IMessageReceiverClientFactory
    {
        Task<IMqttClientWrapper> GetReceiverAsync(MqttClientOptions configuration, CancellationToken cancellationToken = default);
        Task DisposeReceiverAsync(IMqttClientWrapper receiverClient, CancellationToken cancellationToken = default);
    }
}