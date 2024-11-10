namespace Elsa.Kafka;

public interface IConsumerDefinitionEnumerator
{
    /// <summary>
    /// Retrieves a list of consumer definitions provided by <see cref="IConsumerDefinitionProvider"/> implementations.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A list of ConsumerDefinition objects.</returns>
    Task<IEnumerable<ConsumerDefinition>> GetConsumerDefinitionsAsync(CancellationToken cancellationToken);
}