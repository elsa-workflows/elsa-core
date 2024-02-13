using Azure.Messaging.ServiceBus.Administration;
using Elsa.Hosting.Management.Notifications;
using Elsa.MassTransit.AzureServiceBus.Services;
using Elsa.Mediator.Contracts;

namespace Elsa.MassTransit.AzureServiceBus.Handlers;

/// <summary>
/// Class responsible for removing orphaned subscriptions.
/// </summary>
public class OrphanedSubscriptionRemover(
    MessageTopologyProvider topologyProvider,
    ServiceBusAdministrationClient client)
    : INotificationHandler<InstanceDeactivated>
{
    /// <summary>
    /// Removes orphaned subscriptions from Azure Service Bus.
    /// </summary>
    public async Task HandleAsync(InstanceDeactivated notification, CancellationToken cancellationToken)
    {
        var subscriptionTopology = topologyProvider.GetShortLivedSubscriptions().ToList();

        foreach (var subscription in subscriptionTopology)
        {
            // Get subscriptions based on topics instead of the topology since when names are longer than 50 characters
            // MassTransit automatically truncates them.
            await foreach (var asbSubscription in client.GetSubscriptionsAsync(subscription.TopicName,
                               cancellationToken))
            {
                if (asbSubscription.SubscriptionName.StartsWith($"Elsa-{notification.InstanceName}-"))
                {
                    var queueName = asbSubscription.ForwardTo.Substring(asbSubscription.ForwardTo.LastIndexOf('/') + 1);

                    await Task.WhenAll(
                        client.DeleteSubscriptionAsync(subscription.TopicName,
                            asbSubscription.SubscriptionName,
                            cancellationToken),
                        client.DeleteQueueAsync(queueName, cancellationToken));
                }
            }
        }
    }
}