using System.Runtime.CompilerServices;
using Open.Linq.AsyncExtensions;

namespace Elsa.Kafka.Implementations;

public class SchemaRegistryDefinitionEnumerator(IEnumerable<ISchemaRegistryDefinitionProvider> providers) : ISchemaRegistryDefinitionEnumerator
{
    public async Task<IEnumerable<SchemaRegistryDefinition>> EnumerateAsync(CancellationToken cancellationToken)
    {
        return await GetSchemaRegistryDefinitionsInternalAsync(cancellationToken).ToListAsync(cancellationToken);
    }
    
    private async IAsyncEnumerable<SchemaRegistryDefinition> GetSchemaRegistryDefinitionsInternalAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var provider in providers)
        {
            var schemaRegistryDefinitions = await provider.GetSchemaRegistryDefinitionsAsync(cancellationToken).ToList();

            foreach (var schemaRegistryDefinition in schemaRegistryDefinitions)
                yield return schemaRegistryDefinition;
        }
    }
}