using Azure.Messaging.ServiceBus.Administration;
using Elsa.AzureServiceBus.Contracts;

namespace Elsa.AzureServiceBus.Services;

/// <inheritdoc />
public class ServiceBusInitializer : IServiceBusInitializer
{
    private readonly ServiceBusAdministrationClient _serviceBusAdministrationClient;
    private readonly IReadOnlyCollection<IQueueProvider> _queueProviders;
    private readonly IReadOnlyCollection<ITopicProvider> _topicProviders;
    private readonly IReadOnlyCollection<ISubscriptionProvider> _subscriptionProviders;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ServiceBusInitializer(
        ServiceBusAdministrationClient serviceBusAdministrationClient,
        IEnumerable<IQueueProvider> queueProviders,
        IEnumerable<ITopicProvider> topicProviders,
        IEnumerable<ISubscriptionProvider> subscriptionProviders)
    {
        _serviceBusAdministrationClient = serviceBusAdministrationClient;
        _queueProviders = queueProviders.ToList();
        _topicProviders = topicProviders.ToList();
        _subscriptionProviders = subscriptionProviders.ToList();
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var tasks = new[] { CreateQueuesAsync(cancellationToken), CreateTopicsAsync(cancellationToken), CreateSubscriptionsAsync(cancellationToken) };
        await Task.WhenAll(tasks);
    }

    private async Task CreateQueuesAsync(CancellationToken cancellationToken)
    {
        var definitions = (await Task.WhenAll(_queueProviders.Select(async x => await x.GetQueuesAsync(cancellationToken)))).SelectMany(x => x);
        var parallelOptions = new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = 5 };
        await Parallel.ForEachAsync(definitions, parallelOptions, async (definition, ct) =>
        {
            if (!await _serviceBusAdministrationClient.QueueExistsAsync(definition.Name, ct))
                await _serviceBusAdministrationClient.CreateQueueAsync(definition.Name, ct);
        });
    }

    private async Task CreateTopicsAsync(CancellationToken cancellationToken)
    {
        var definitions = (await Task.WhenAll(_topicProviders.Select(async x => await x.GetTopicsAsync(cancellationToken)))).SelectMany(x => x);
        var parallelOptions = new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = 5 };
        await Parallel.ForEachAsync(definitions, parallelOptions, async (definition, ct) =>
        {
            if (!await _serviceBusAdministrationClient.TopicExistsAsync(definition.Name, ct))
                await _serviceBusAdministrationClient.CreateTopicAsync(definition.Name, ct);
        });
    }

    private async Task CreateSubscriptionsAsync(CancellationToken cancellationToken)
    {
        var definitions = (await Task.WhenAll(_subscriptionProviders.Select(async x => await x.GetSubscriptionsAsync(cancellationToken)))).SelectMany(x => x);
        var parallelOptions = new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = 5 };
        await Parallel.ForEachAsync(definitions, parallelOptions, async (definition, ct) =>
        {
            if (!await _serviceBusAdministrationClient.SubscriptionExistsAsync(definition.Topic, definition.Name, ct))
                await _serviceBusAdministrationClient.CreateSubscriptionAsync(definition.Topic, definition.Name, ct);
        });
    }
}