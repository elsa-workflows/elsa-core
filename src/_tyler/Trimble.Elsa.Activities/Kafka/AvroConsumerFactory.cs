using Confluent.SchemaRegistry;
using Elsa.Kafka;
using Elsa.Kafka.Implementations;

namespace Trimble.Elsa.Activities.Kafka;
public class AvroConsumerFactory<TK, TV> : IConsumerFactory
{
    public IConsumer CreateConsumer(CreateConsumerContext workerContext)
    {
        var consumerDefinition = workerContext.ConsumerDefinition;

        if (consumerDefinition == null)
            throw new InvalidOperationException("Consumer definition is required for Avro consumer.");

        var schemaDefinition = workerContext.SchemaRegistryDefinition;

        if (schemaDefinition == null)
            throw new InvalidOperationException($"Schema registry definition is required for Avro consumer. There was an issue creating {consumerDefinition.SchemaRegistryId}.");

        CachedSchemaRegistryClient schemaRegistryClient = new(schemaDefinition.Config);

        var consumer = new ConsumerBuilderWithSerialization<TK, TV>(consumerDefinition.Config)
            .SetKeyValueDeserializers(schemaRegistryClient)
            .Build();

        return new ConsumerProxy(consumer);
    }
}
