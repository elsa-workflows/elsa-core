namespace Elsa.ServerAndStudio.Web.Enums;

/// Represents the type of messaging broker used in MassTransit.
public enum MassTransitBroker
{
    Memory,
    AzureServiceBus,
    RabbitMq
}