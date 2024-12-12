namespace Elsa.Kafka;

public interface IProducerDefinitionEnumerator
{
    /// <summary>
    /// Retrieves a list of producer definitions provided by <see cref="IProducerDefinitionProvider"/> implementations.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A list of ProducerDefinition objects.</returns>
    Task<IEnumerable<ProducerDefinition>> EnumerateAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a producer definition by its ID.
    /// </summary>
    Task<ProducerDefinition> GetByIdAsync(string id);
}