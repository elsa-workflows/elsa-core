using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Kafka.Configuration;

namespace Elsa.Activities.Kafka.Services
{
    public interface IMessageReceiverClientFactory
    {
        Task<IClient> GetReceiverAsync(KafkaConfiguration config, CancellationToken cancellationToken = default);
        Task DisposeReceiverAsync(IClient receiverClient, CancellationToken cancellationToken = default);
    }
}