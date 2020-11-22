using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus.Core;

namespace Elsa.Activities.AzureServiceBus.Services
{
    public interface IServiceBusFactory
    {
        Task<IMessageSender> GetSenderAsync(string queueName, CancellationToken cancellationToken = default);
        Task<IMessageReceiver> GetReceiverAsync(string queueName, CancellationToken cancellationToken = default);
    }
}