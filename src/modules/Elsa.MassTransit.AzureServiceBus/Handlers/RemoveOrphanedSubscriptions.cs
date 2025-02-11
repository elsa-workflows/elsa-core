using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Elsa.Hosting.Management.Notifications;
using Elsa.MassTransit.AzureServiceBus.Services;
using Elsa.Mediator.Contracts;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Elsa.MassTransit.AzureServiceBus.Handlers;

/// <summary>
/// A handler for the <see cref="HeartbeatTimedOut"/> notification that removes subscriptions from Azure Service Bus
/// when there has not been any heartbeat received lately for this instance.
/// </summary>
[UsedImplicitly]
public class RemoveOrphanedSubscriptions(MessageTopologyProvider topologyProvider,
    ServiceBusAdministrationClient client,
    ILogger<RemoveOrphanedSubscriptions> logger) 
    : INotificationHandler<HeartbeatTimedOut>
{
    /// <summary>
    /// Removes orphaned subscriptions from Azure Service Bus.
    /// </summary>
    public async Task HandleAsync(HeartbeatTimedOut notification, CancellationToken cancellationToken)
    {
        var subscriptionTopology = topologyProvider.GetTemporarySubscriptions().ToList();

        foreach (var subscription in subscriptionTopology)
        {
            try
            {
                // Get subscriptions based on topics instead of the topology since when names are longer than 50 characters
                // MassTransit automatically truncates them.
                await foreach (var asbSubscription in client.GetSubscriptionsAsync(subscription.TopicName, cancellationToken))
                {
                    if (asbSubscription.SubscriptionName.StartsWith($"{notification.InstanceName}-elsa-"))
                    {
                        var queueName = asbSubscription.ForwardTo[(asbSubscription.ForwardTo.LastIndexOf('/') + 1)..];

                        await Task.WhenAll(
                            client.DeleteSubscriptionAsync(subscription.TopicName, asbSubscription.SubscriptionName, cancellationToken),
                            client.DeleteQueueAsync(queueName, cancellationToken));
                    }
                }
            }
            catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
            {
                logger.LogInformation("Cannot remove subscription {Subscription} of topic {Topic} because entity {EntityPath} was not found. The subscription has already been removed.", subscription.SubscriptionName, subscription.TopicName, ex.EntityPath);
            }
        }
    }
}