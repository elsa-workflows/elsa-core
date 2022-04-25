using Elsa.Modules.AzureServiceBus.Models;

namespace Elsa.Modules.AzureServiceBus.Services;

/// <summary>
/// Provides topic definitions to the system. 
/// </summary>
public interface ITopicProvider
{
    ValueTask<ICollection<TopicDefinition>> GetTopicsAsync(CancellationToken cancellationToken);
}