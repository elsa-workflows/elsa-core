using System.Runtime.CompilerServices;
using Open.Linq.AsyncExtensions;

namespace Elsa.Kafka.Implementations;

public class ConsumerDefinitionEnumerator(IEnumerable<IConsumerDefinitionProvider> providers) : IConsumerDefinitionEnumerator
{
    public async Task<IEnumerable<ConsumerDefinition>> EnumerateAsync(CancellationToken cancellationToken)
    {
        return await GetConsumerDefinitionsInternalAsync(cancellationToken).ToListAsync(cancellationToken);
    }
    
    private async IAsyncEnumerable<ConsumerDefinition> GetConsumerDefinitionsInternalAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var provider in providers)
        {
            var consumerDefinitions = await provider.GetConsumerDefinitionsAsync(cancellationToken).ToList();

            foreach (var consumerDefinition in consumerDefinitions)
                yield return consumerDefinition;
        }
    }
}