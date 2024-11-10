using System.Runtime.CompilerServices;
using Open.Linq.AsyncExtensions;

namespace Elsa.Kafka;

public class ProducerDefinitionEnumerator(IEnumerable<IProducerDefinitionProvider> providers) : IProducerDefinitionEnumerator
{
    public async Task<IEnumerable<ProducerDefinition>> EnumerateAsync(CancellationToken cancellationToken)
    {
        return await GetProducerDefinitionsInternalAsync(cancellationToken).ToListAsync(cancellationToken);
    }

    public async Task<ProducerDefinition> GetByIdAsync(string id)
    {
        var producerDefinitions = await GetProducerDefinitionsInternalAsync(CancellationToken.None).ToListAsync();
        return producerDefinitions.FirstOrDefault(x => x.Id == id) ?? throw new InvalidOperationException($"Producer definition with ID '{id}' not found.");
    }

    private async IAsyncEnumerable<ProducerDefinition> GetProducerDefinitionsInternalAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var provider in providers)
        {
            var producerDefinitions = await provider.GetProducerDefinitionsAsync(cancellationToken).ToList();

            foreach (var producerDefinition in producerDefinitions)
                yield return producerDefinition;
        }
    }
}