namespace Elsa.AzureServiceBus.Models;

/// <summary>
/// Represents a topic that is available to the system.
/// </summary>
public class TopicDefinition
{
    /// <summary>
    /// The topic name.
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// The subscriptions.
    /// </summary>
    public ICollection<SubscriptionDefinition> Subscriptions { get; set; } = new List<SubscriptionDefinition>();
}