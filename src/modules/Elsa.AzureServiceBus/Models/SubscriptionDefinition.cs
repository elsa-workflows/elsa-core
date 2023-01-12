namespace Elsa.AzureServiceBus.Models;

/// <summary>
/// Represents a topic subscription that is available to the system.
/// </summary>
public class SubscriptionDefinition
{
    /// <summary>
    /// The subscription name.
    /// </summary>
    public string Name { get; set; } = default!;
    
    /// <summary>
    /// The topic.
    /// </summary>
    public string Topic { get; set; } = default!;
}