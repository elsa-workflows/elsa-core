namespace Elsa.ServiceBus.Kafka;

public interface IProducerDefinitionProvider
{
    Task<IEnumerable<ProducerDefinition>> GetProducerDefinitionsAsync(CancellationToken cancellationToken = default);
}