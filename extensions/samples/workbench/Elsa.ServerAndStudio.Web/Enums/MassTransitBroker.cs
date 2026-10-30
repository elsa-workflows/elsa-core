namespace Elsa.ServerAndStudio.Web.Enums;

/// <summary>
/// Represents the type of messaging broker used in MassTransit.
/// </summary>
public enum MassTransitBroker
{
    Memory,
    AzureServiceBus,
    RabbitMq
}