namespace Elsa.AzureServiceBus.Models;

/// <summary>
/// Represents a topic that is available to the system.
/// </summary>
public class TopicDefinition
{
    /// The topic name.
    public string Name { get; set; } = default!;

    /// The subscriptions.
    public ICollection<SubscriptionDefinition> Subscriptions { get; set; } = new List<SubscriptionDefinition>();
}