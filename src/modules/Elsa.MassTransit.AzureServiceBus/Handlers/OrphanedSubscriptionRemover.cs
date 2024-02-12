using Azure.Messaging.ServiceBus.Administration;
using Elsa.Hosting.Management.Notifications;
using Elsa.MassTransit.AzureServiceBus.Services;
using Elsa.Mediator.Contracts;

namespace Elsa.MassTransit.AzureServiceBus.Handlers;

/// <summary>
/// Class responsible for removing orphaned subscriptions.
/// </summary>
public class OrphanedSubscriptionRemover(MessageTopologyProvider topologyProvider, ServiceBusAdministrationClient client) 
    : INotificationHandler<InstanceDeactivated>
{
    /// <summary>
    /// Removes orphaned subscriptions from Azure Service Bus.
    /// </summary>
    public async Task HandleAsync(InstanceDeactivated notification, CancellationToken cancellationToken)
    {
        var subscriptions = topologyProvider.GetShortLivedSubscriptions().ToList();
        
        foreach (var subscription in subscriptions)
        {
            await client.DeleteSubscriptionAsync(subscription.TopicName,
                $"Elsa-{notification.InstanceName}-{subscription.SubscriptionName}",
                cancellationToken);
        }
    }
}