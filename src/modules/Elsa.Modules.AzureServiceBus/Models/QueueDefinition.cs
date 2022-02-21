namespace Elsa.Modules.AzureServiceBus.Models;

/// <summary>
/// Represents a queue that is available to the system.
/// </summary>
public class QueueDefinition
{
    public string Name { get; set; } = default!;
}