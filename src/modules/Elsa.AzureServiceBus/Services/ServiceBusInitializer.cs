using Azure.Messaging.ServiceBus.Administration;
using Elsa.AzureServiceBus.Contracts;

namespace Elsa.AzureServiceBus.Services;

/// <inheritdoc />
public class ServiceBusInitializer(
    ServiceBusAdministrationClient serviceBusAdministrationClient,
    IEnumerable<IQueueProvider> queueProviders,
    IEnumerable<ITopicProvider> topicProviders,
    IEnumerable<ISubscriptionProvider> subscriptionProviders)
    : IServiceBusInitializer
{
    private readonly IReadOnlyCollection<IQueueProvider> _queueProviders = queueProviders.ToList();
    private readonly IReadOnlyCollection<ITopicProvider> _topicProviders = topicProviders.ToList();
    private readonly IReadOnlyCollection<ISubscriptionProvider> _subscriptionProviders = subscriptionProviders.ToList();

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var tasks = new[]
        {
            CreateQueuesAsync(cancellationToken), 
            CreateTopicsAsync(cancellationToken),
            CreateSubscriptionsAsync(cancellationToken)
        };
        await Task.WhenAll(tasks);
    }

    private async Task CreateQueuesAsync(CancellationToken cancellationToken)
    {
        var definitions = (await Task.WhenAll(_queueProviders.Select(async x => await x.GetQueuesAsync(cancellationToken)))).SelectMany(x => x);
        var parallelOptions = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = 5
        };
        await Parallel.ForEachAsync(definitions, parallelOptions, async (definition, ct) =>
        {
            if (!await serviceBusAdministrationClient.QueueExistsAsync(definition.Name, ct))
                await serviceBusAdministrationClient.CreateQueueAsync(definition.Name, ct);
        });
    }

    private async Task CreateTopicsAsync(CancellationToken cancellationToken)
    {
        var definitionTasks = _topicProviders.Select(async x => await x.GetTopicsAsync(cancellationToken));
        var definitions = (await Task.WhenAll(definitionTasks)).SelectMany(x => x).ToList();

        if (!definitions.Any())
            return;

        var parallelOptions = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = 5
        };
        await Parallel.ForEachAsync(definitions, parallelOptions, async (topic, ct) =>
        {
            if (!await serviceBusAdministrationClient.TopicExistsAsync(topic.Name, ct))
                await serviceBusAdministrationClient.CreateTopicAsync(topic.Name, ct);

            foreach (var subscription in topic.Subscriptions)
            {
                if (!await serviceBusAdministrationClient.SubscriptionExistsAsync(topic.Name, subscription.Name, ct))
                    await serviceBusAdministrationClient.CreateSubscriptionAsync(topic.Name, subscription.Name, ct);
            }
        });
    }

    private async Task CreateSubscriptionsAsync(CancellationToken cancellationToken)
    {
        var definitionTasks = _subscriptionProviders.Select(async x => await x.GetSubscriptionsAsync(cancellationToken));
        var definitions = (await Task.WhenAll(definitionTasks)).SelectMany(x => x).ToList();

        if (!definitions.Any())
            return;

        var parallelOptions = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = 5
        };
        await Parallel.ForEachAsync(definitions, parallelOptions, async (definition, ct) =>
        {
            if (!await serviceBusAdministrationClient.SubscriptionExistsAsync(definition.Topic, definition.Name, ct))
                await serviceBusAdministrationClient.CreateSubscriptionAsync(definition.Topic, definition.Name, ct);
        });
    }
}