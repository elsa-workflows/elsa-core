using Elsa.AzureServiceBus.Models;

namespace Elsa.AzureServiceBus.Options;

/// <summary>
/// A collection of settings to configure integration with Azure Service Bus. 
/// </summary>
public class AzureServiceBusOptions
{
    /// <summary>
    /// Th connection string or connection string name to connect with the service bus.
    /// </summary>
    public string ConnectionStringOrName { get; set; } = default!;
    
    /// <summary>
    /// A list of <see cref="QueueDefinition"/>s to create.
    /// </summary>
    public ICollection<QueueDefinition> Queues { get; set; } = new List<QueueDefinition>();
    
    /// <summary>
    /// A list of <see cref="TopicDefinition"/>s to create.
    /// </summary>
    public ICollection<TopicDefinition> Topics { get; set; } = new List<TopicDefinition>();
    
    /// <summary>
    /// A list of <see cref="SubscriptionDefinition"/>s to create.
    /// </summary>
    public ICollection<SubscriptionDefinition> Subscriptions { get; set; } = new List<SubscriptionDefinition>();
}