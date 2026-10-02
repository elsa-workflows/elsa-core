namespace Elsa.ServiceBus.Kafka;

public interface ITopicDefinitionProvider
{
    Task<IEnumerable<TopicDefinition>> GetTopicDefinitionsAsync(CancellationToken cancellationToken = default);
}