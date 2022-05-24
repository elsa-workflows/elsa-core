using Elsa.AzureServiceBus.Models;

namespace Elsa.AzureServiceBus.Services;

/// <summary>
/// Provides topic definitions to the system. 
/// </summary>
public interface ITopicProvider
{
    ValueTask<ICollection<TopicDefinition>> GetTopicsAsync(CancellationToken cancellationToken);
}