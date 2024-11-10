using System.Runtime.CompilerServices;
using Open.Linq.AsyncExtensions;

namespace Elsa.Kafka;

public class ConsumerDefinitionEnumerator(IEnumerable<IConsumerDefinitionProvider> providers) : IConsumerDefinitionEnumerator
{
    public async Task<IEnumerable<ConsumerDefinition>> GetConsumerDefinitionsAsync(CancellationToken cancellationToken)
    {
        return await GetConsumerDefinitionsInternalAsync(cancellationToken).ToListAsync(cancellationToken);
    }
    
    private async IAsyncEnumerable<ConsumerDefinition> GetConsumerDefinitionsInternalAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var provider in providers)
        {
            var consumerDefinitions = await provider.GetConsumerConfigsAsync(cancellationToken).ToList();

            foreach (var consumerDefinition in consumerDefinitions)
                yield return consumerDefinition;
        }
    }
}