namespace Elsa.MassTransit.AzureServiceBus.Models;

/// <summary>
/// Represents the topology of a message subscription in Azure Service Bus.
/// </summary>
public record MessageSubscriptionTopology(string TopicName, string SubscriptionName, bool IsTemporary);