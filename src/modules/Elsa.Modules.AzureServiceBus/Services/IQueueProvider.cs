using Elsa.Modules.AzureServiceBus.Models;

namespace Elsa.Modules.AzureServiceBus.Services;

/// <summary>
/// Provides queue definitions to the system. 
/// </summary>
public interface IQueueProvider
{
    ValueTask<ICollection<QueueDefinition>> GetQueuesAsync(CancellationToken cancellationToken);
}