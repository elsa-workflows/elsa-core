using Microsoft.Extensions.Options;

namespace Elsa.Kafka.Providers;

public class OptionsDefinitionProvider(IOptions<KafkaOptions> options) : IConsumerDefinitionProvider, IProducerDefinitionProvider, ITopicDefinitionProvider
{
    public Task<IEnumerable<ConsumerDefinition>> GetConsumerDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<ConsumerDefinition>>(options.Value.Consumers);
    }

    public Task<IEnumerable<ProducerDefinition>> GetProducerDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<ProducerDefinition>>(options.Value.Producers);
    }

    public Task<IEnumerable<TopicDefinition>> GetTopicDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<TopicDefinition>>(options.Value.Topics);
    }
}