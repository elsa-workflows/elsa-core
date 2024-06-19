using Elsa.Mediator.Contracts;

namespace Elsa.MassTransit.AzureServiceBus.Notifications;

/// <summary>
/// Notification to clean up the Azure Service Bus subscriptions.
/// </summary>
public record CleanupSubscriptions : INotification;