namespace Elsa.Kafka;

public interface ITopicDefinitionEnumerator
{
    /// <summary>
    /// Retrieves a list of topic definitions provided by <see cref="ITopicDefinitionProvider"/> implementations.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A list of TopicDefinition objects.</returns>
    Task<IEnumerable<TopicDefinition>> EnumerateAsync(CancellationToken cancellationToken);
}