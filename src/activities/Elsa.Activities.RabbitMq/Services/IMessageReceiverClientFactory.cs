using Elsa.Activities.RabbitMq.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.RabbitMq.Services
{
    public interface IMessageReceiverClientFactory
    {
        Task<IClient> GetReceiverAsync(RabbitMqBusConfiguration config, CancellationToken cancellationToken = default);
        Task DisposeReceiverAsync(IClient receiverClient, CancellationToken cancellationToken = default);
    }
}