using System.Runtime.CompilerServices;
using Open.Linq.AsyncExtensions;

namespace Elsa.Kafka;

public class TopicDefinitionEnumerator(IEnumerable<ITopicDefinitionProvider> providers) : ITopicDefinitionEnumerator
{
    public async Task<IEnumerable<TopicDefinition>> EnumerateAsync(CancellationToken cancellationToken)
    {
        return await GetTopicDefinitionsInternalAsync(cancellationToken).ToListAsync(cancellationToken);
    }
    
    private async IAsyncEnumerable<TopicDefinition> GetTopicDefinitionsInternalAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var provider in providers)
        {
            var topicDefinitions = await provider.GetTopicDefinitionsAsync(cancellationToken).ToList();

            foreach (var topicDefinition in topicDefinitions)
                yield return topicDefinition;
        }
    }
}