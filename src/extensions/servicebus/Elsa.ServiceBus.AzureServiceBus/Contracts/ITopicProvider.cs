using Elsa.ServiceBus.AzureServiceBus.Models;

namespace Elsa.ServiceBus.AzureServiceBus.Contracts;

/// <summary>
/// Provides topic definitions to the system. 
/// </summary>
public interface ITopicProvider
{
    /// <summary>
    /// Returns a list of <see cref="TopicDefinition"/>s.
    /// </summary>
    ValueTask<ICollection<TopicDefinition>> GetTopicsAsync(CancellationToken cancellationToken);
}