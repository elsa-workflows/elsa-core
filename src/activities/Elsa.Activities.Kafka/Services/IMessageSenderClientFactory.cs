using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Kafka.Configuration;

namespace Elsa.Activities.Kafka.Services
{
    public interface IMessageSenderClientFactory
    {
        Task<IClient> GetSenderAsync(KafkaConfiguration config, CancellationToken cancellationToken = default);
        Task DisposeSenderAsync(IClient senderClient, CancellationToken cancellationToken = default);
    }
}