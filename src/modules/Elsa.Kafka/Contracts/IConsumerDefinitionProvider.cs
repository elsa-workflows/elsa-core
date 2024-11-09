namespace Elsa.Kafka;

public interface IConsumerDefinitionProvider
{
    Task<IEnumerable<ConsumerDefinition>> GetConsumerConfigsAsync(CancellationToken cancellationToken = default);
}