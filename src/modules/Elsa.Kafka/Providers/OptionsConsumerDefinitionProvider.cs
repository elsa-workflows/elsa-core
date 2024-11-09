using Microsoft.Extensions.Options;

namespace Elsa.Kafka;

public class OptionsConsumerDefinitionProvider(IOptions<KafkaOptions> options) : IConsumerDefinitionProvider
{
    public Task<IEnumerable<ConsumerDefinition>> GetConsumerConfigsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<ConsumerDefinition>>(options.Value.ConsumerDefinitions);
    }
}