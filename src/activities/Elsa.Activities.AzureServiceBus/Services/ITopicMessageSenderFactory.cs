using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus.Core;

namespace Elsa.Activities.AzureServiceBus.Services
{
    public interface ITopicMessageSenderFactory
    {
        Task<ISenderClient> GetTopicSenderAsync(string topicName, CancellationToken cancellationToken = default);
    }
}