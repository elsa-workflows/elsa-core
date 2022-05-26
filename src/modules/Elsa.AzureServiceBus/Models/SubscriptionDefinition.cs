namespace Elsa.AzureServiceBus.Models;

/// <summary>
/// Represents a topic subscription that is available to the system.
/// </summary>
public class SubscriptionDefinition
{
    public string Name { get; set; } = default!;
    public string Topic { get; set; } = default!;
}