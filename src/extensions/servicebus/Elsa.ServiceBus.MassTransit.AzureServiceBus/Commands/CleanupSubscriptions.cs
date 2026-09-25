using Elsa.Mediator.Contracts;

namespace Elsa.ServiceBus.MassTransit.AzureServiceBus.Commands;

/// <summary>
/// Notification to clean up the Azure Service Bus subscriptions.
/// </summary>
public record CleanupSubscriptions : ICommand;