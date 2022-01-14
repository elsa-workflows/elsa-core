using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.RabbitMq.Configuration;

namespace Elsa.Activities.RabbitMq.Services
{
    public interface IMessageReceiverClientFactory
    {
        Task<IClient> GetReceiverAsync(RabbitMqBusConfiguration config, CancellationToken cancellationToken = default);
        Task DisposeReceiverAsync(IClient receiverClient, CancellationToken cancellationToken = default);
    }
}