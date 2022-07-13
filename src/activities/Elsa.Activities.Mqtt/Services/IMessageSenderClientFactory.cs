using Elsa.Activities.Mqtt.Options;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.Mqtt.Services
{
    public interface IMessageSenderClientFactory
    {
        Task<IMqttClientWrapper> GetSenderAsync(MqttClientOptions configuration, CancellationToken cancellationToken = default);
    }
}