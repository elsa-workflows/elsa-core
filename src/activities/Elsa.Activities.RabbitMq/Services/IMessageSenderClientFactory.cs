using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.RabbitMq.Configuration;

namespace Elsa.Activities.RabbitMq.Services
{
    public interface IMessageSenderClientFactory
    {
        Task<IClient> GetSenderAsync(RabbitMqBusConfiguration config, CancellationToken cancellationToken = default);
        Task DisposeSenderAsync(IClient senderClient, CancellationToken cancellationToken = default);
    }
}