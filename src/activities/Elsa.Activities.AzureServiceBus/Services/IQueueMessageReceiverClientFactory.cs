using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus.Core;

namespace Elsa.Activities.AzureServiceBus.Services
{
    public interface IQueueMessageReceiverClientFactory
    {
        Task<IReceiverClient> GetReceiverAsync(string queueName, CancellationToken cancellationToken = default);
        Task DisposeReceiverAsync(IReceiverClient receiverClient, CancellationToken cancellationToken = default);
    }
}