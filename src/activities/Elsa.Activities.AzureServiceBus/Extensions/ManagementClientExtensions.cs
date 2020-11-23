using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus.Management;

namespace Elsa.Activities.AzureServiceBus.Extensions
{
    public static class ManagementClientExtensions
    {
        public static async Task EnsureQueueExistsAsync(this ManagementClient managementClient, string queueName, CancellationToken cancellationToken)
        {
            if (await managementClient.QueueExistsAsync(queueName, cancellationToken))
                return;

            await managementClient.CreateQueueAsync(queueName, cancellationToken);
        }
    }
}