namespace Elsa.Kafka;

public interface ITopicDefinitionProvider
{
    Task<IEnumerable<TopicDefinition>> GetTopicDefinitionsAsync(CancellationToken cancellationToken = default);
}