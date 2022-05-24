using Elsa.AzureServiceBus.Models;

namespace Elsa.AzureServiceBus.Services;

/// <summary>
/// Provides queue definitions to the system. 
/// </summary>
public interface IQueueProvider
{
    ValueTask<ICollection<QueueDefinition>> GetQueuesAsync(CancellationToken cancellationToken);
}