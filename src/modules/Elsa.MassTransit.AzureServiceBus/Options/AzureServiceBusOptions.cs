namespace Elsa.MassTransit.AzureServiceBus.Options;

/// <summary>
/// A collection of settings to configure integration with Azure Service Bus. 
/// </summary>
public class AzureServiceBusOptions
{
    /// <summary>
    /// Th connection string or connection string name to connect with the service bus.
    /// </summary>
    public string? ConnectionStringOrName { get; set; }
}