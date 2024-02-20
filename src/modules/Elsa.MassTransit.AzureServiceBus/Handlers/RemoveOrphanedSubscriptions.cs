using Azure.Messaging.ServiceBus.Administration;
using Elsa.Hosting.Management.Notifications;
using Elsa.MassTransit.AzureServiceBus.Services;
using Elsa.Mediator.Contracts;
using JetBrains.Annotations;

namespace Elsa.MassTransit.AzureServiceBus.Handlers;

/// <summary>
/// A handler for the <see cref="HeartbeatTimedOut"/> notification that removes orphaned subscriptions from Azure Service Bus.
/// </summary>
[UsedImplicitly]
public class RemoveOrphanedSubscriptions(MessageTopologyProvider topologyProvider, ServiceBusAdministrationClient client) : INotificationHandler<HeartbeatTimedOut>
{
    /// <summary>
    /// Removes orphaned subscriptions from Azure Service Bus.
    /// </summary>
    public async Task HandleAsync(HeartbeatTimedOut notification, CancellationToken cancellationToken)
    {
        var subscriptionTopology = topologyProvider.GetTemporarySubscriptions().ToList();

        foreach (var subscription in subscriptionTopology)
        {
            // Get subscriptions based on topics instead of the topology since when names are longer than 50 characters.
            // MassTransit automatically truncates them.
            await foreach (var asbSubscription in client.GetSubscriptionsAsync(subscription.TopicName, cancellationToken))
            {
                if (asbSubscription.SubscriptionName.StartsWith($"Elsa-{notification.InstanceName}-"))
                {
                    var queueName = asbSubscription.ForwardTo[(asbSubscription.ForwardTo.LastIndexOf('/') + 1)..];

                    await Task.WhenAll(
                        client.DeleteSubscriptionAsync(subscription.TopicName, asbSubscription.SubscriptionName, cancellationToken),
                        client.DeleteQueueAsync(queueName, cancellationToken));
                }
            }
        }
    }
}