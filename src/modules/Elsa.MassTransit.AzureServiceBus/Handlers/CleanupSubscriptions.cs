using Azure.Messaging.ServiceBus.Administration;
using Elsa.MassTransit.AzureServiceBus.Notifications;
using Elsa.Mediator.Contracts;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Elsa.MassTransit.AzureServiceBus.Handlers;

/// <summary>
/// Cleans up Azure Service Bus resources for which no children can be found.
/// </summary>
/// <remarks>
/// This will clean up topics without subscriptions and subscription of which the queue can not be found.
/// - This handler is unable to differentiate between topologies created by Elsa and others. It will clean up all topology in the namespace.
/// - Queues in other namespaces will not be found by this handler and the subscription will therefore be removed.
/// </remarks>
[UsedImplicitly]
public class CleanupOrphanedTopology(ServiceBusAdministrationClient client, ILogger<CleanupOrphanedTopology> logger) : INotificationHandler<CleanupSubscriptions>
{
    /// <inheritdoc />
    public async Task HandleAsync(CleanupSubscriptions notification, CancellationToken cancellationToken)
    {
        var queues = await client.GetQueuesAsync().ToListAsync(cancellationToken);
        var queueNames = queues.Select(q => q.Name).ToList();
        
        await foreach (var topic in client.GetTopicsAsync(cancellationToken))
        {
            // Only clean up any topics that have the elsa prefix.
            if (!topic.Name.StartsWith("elsa"))
                continue;
            
            var subscriptions = await client.GetSubscriptionsAsync(topic.Name, cancellationToken).ToListAsync(cancellationToken);

            // Delete topics which have no active subscriptions.
            if (subscriptions.Count == 0)
            {
                logger.LogWarning("Deleting topic {name}", topic.Name);
                await client.DeleteTopicAsync(topic.Name, cancellationToken);
                continue;
            }
            
            var subscriptionsToDelete = subscriptions.Where(sub =>
            {
                var queueName = sub.ForwardTo[(sub.ForwardTo.LastIndexOf('/') + 1)..];
                return !queueNames.Contains(queueName);
            }).ToList();
            
            // Remove subscriptions for which the queue to be forwarded to can not be found in the same namespace.
            foreach (SubscriptionProperties? asbSubscription in subscriptionsToDelete)
            {
                logger.LogWarning("Deleting subscription {subscriptionName} on topic {topicName}", asbSubscription.SubscriptionName, topic.Name);
                await client.DeleteSubscriptionAsync(topic.Name, asbSubscription.SubscriptionName, cancellationToken);
            }
        }
    }
}
