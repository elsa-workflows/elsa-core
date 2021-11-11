using Elsa.Activities.RabbitMq.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.RabbitMq.Services
{
    public interface IMessageSenderClientFactory
    {
        Task<IClient> GetSenderAsync(RabbitMqBusConfiguration config, CancellationToken cancellationToken = default);
        Task DisposeSenderAsync(IClient senderClient, CancellationToken cancellationToken = default);
    }
}