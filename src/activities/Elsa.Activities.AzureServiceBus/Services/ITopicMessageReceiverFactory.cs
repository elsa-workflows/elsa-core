using Microsoft.Azure.ServiceBus.Core;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.AzureServiceBus.Services
{
    public interface ITopicMessageReceiverFactory
    {
        Task<IReceiverClient> GetTopicReceiverAsync(string topicName, string subscriptionName, CancellationToken cancellationToken = default);
        Task DisposeReceiverAsync(IReceiverClient receiverClient, CancellationToken cancellationToken = default);
    }
}