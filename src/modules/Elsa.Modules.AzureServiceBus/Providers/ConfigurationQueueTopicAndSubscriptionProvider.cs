using Elsa.Modules.AzureServiceBus.Contracts;
using Elsa.Modules.AzureServiceBus.Models;
using Elsa.Modules.AzureServiceBus.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Modules.AzureServiceBus.Providers;

/// <summary>
/// Represents a queue provider that reads queue definitions from configuration.
/// </summary>
public class ConfigurationQueueTopicAndSubscriptionProvider : IQueueProvider, ITopicProvider, ISubscriptionProvider
{
    private readonly AzureServiceBusOptions _options;
    public ConfigurationQueueTopicAndSubscriptionProvider(IOptions<AzureServiceBusOptions> options) => _options = options.Value;
    public ValueTask<ICollection<QueueDefinition>> GetQueuesAsync(CancellationToken cancellationToken) => new(_options.Queues);
    public ValueTask<ICollection<TopicDefinition>> GetTopicsAsync(CancellationToken cancellationToken) => new(_options.Topics);
    public ValueTask<ICollection<SubscriptionDefinition>> GetSubscriptionsAsync(CancellationToken cancellationToken) => new(_options.Subscriptions);
}