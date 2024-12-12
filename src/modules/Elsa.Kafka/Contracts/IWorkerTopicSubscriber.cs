namespace Elsa.Kafka;

public interface IWorkerTopicSubscriber
{
    /// <summary>
    /// Updates all workers by subscribing to topics based on the current workflow trigger indexes and bookmarks.
    /// </summary>
    Task UpdateTopicSubscriptionsAsync(CancellationToken cancellationToken);
}