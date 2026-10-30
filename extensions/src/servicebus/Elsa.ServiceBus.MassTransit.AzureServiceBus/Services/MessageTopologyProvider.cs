using Elsa.ServiceBus.MassTransit.AzureServiceBus.Models;

namespace Elsa.ServiceBus.MassTransit.AzureServiceBus.Services;

/// <summary>
/// Provides message topology information.
/// </summary>
public class MessageTopologyProvider
{
    private readonly IEnumerable<MessageSubscriptionTopology> _subscriptionTopology;

    /// <summary>
    /// Provides message topology information.
    /// </summary>
    public MessageTopologyProvider(IEnumerable<MessageSubscriptionTopology> subscriptionTopology)
    {
        _subscriptionTopology = subscriptionTopology;
    }

    /// <summary>
    /// Retrieves all the temporary message subscriptions from the subscription topology.
    /// </summary>
    public IEnumerable<MessageSubscriptionTopology> GetTemporarySubscriptions()
    {
        return _subscriptionTopology.Where(x => x.IsTemporary);
    }
}