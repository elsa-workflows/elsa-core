using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus.Core;

namespace Elsa.Activities.AzureServiceBus.Services
{
    public interface ITopicMessageSenderFactory
    {
        Task<IMessageSender> GetTopicSenderAsync(string topicName, CancellationToken cancellationToken = default);
    }
}