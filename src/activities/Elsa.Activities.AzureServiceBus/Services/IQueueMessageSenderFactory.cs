using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus.Core;

namespace Elsa.Activities.AzureServiceBus.Services
{
    public interface IQueueMessageSenderFactory
    {
        Task<ISenderClient> GetSenderAsync(string queueName, CancellationToken cancellationToken = default);
    }
}