namespace Elsa.Kafka;

public interface ISchemaRegistryDefinitionEnumerator
{
    /// <summary>
    /// Retrieves a list of schema registry definitions provided by <see cref="ISchemaRegistryDefinitionProvider"/> implementations.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A list of SchemaRegistryDefinition objects.</returns>
    Task<IEnumerable<SchemaRegistryDefinition>> EnumerateAsync(CancellationToken cancellationToken);
}