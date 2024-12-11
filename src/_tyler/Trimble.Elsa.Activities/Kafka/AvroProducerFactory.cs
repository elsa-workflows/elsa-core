using Confluent.SchemaRegistry;
using Elsa.Kafka;
using Elsa.Kafka.Implementations;

namespace Trimble.Elsa.Activities.Kafka;

public class AvroProducerFactory<TK, TV> : IProducerFactory 
{
    public IProducer CreateProducer(CreateProducerContext workerContext)
    {
        var producerDefinition = workerContext.ProducerDefinition;

        if (producerDefinition == null)
            throw new InvalidOperationException("Consumer definition is required for Avro consumer.");

        var schemaDefinition = workerContext.SchemaRegistryDefinition;

        if (schemaDefinition == null)
            throw new InvalidOperationException($"Schema registry definition is required for Avro consumer. There was an issue creating {producerDefinition.SchemaRegistryId}.");

        CachedSchemaRegistryClient schemaRegistryClient = new(schemaDefinition.Config);

        var producer = new ProducerBuilderWithSerialization<TK, TV>(producerDefinition.Config)
           .SetKeyValueSerializers(schemaRegistryClient)
           .Build();
        ProducerProxy proxy = new ProducerProxy(producer);

        return proxy;
    }
}
