namespace Elsa.AzureServiceBus.Contracts;

/// <summary>
/// Creates queues, topics and subscriptions provided by <see cref="IQueueProvider"/>, <see cref="ITopicProvider"/> and <see cref="ISubscriptionProvider"/> implementations.
/// </summary>
public interface IServiceBusInitializer
{
    /// <summary>
    /// Creates queues, topics and subscriptions provided by <see cref="IQueueProvider"/>, <see cref="ITopicProvider"/> and <see cref="ISubscriptionProvider"/> implementations.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);
}