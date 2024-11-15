using Elsa.Kafka.Serialization;

namespace Elsa.Kafka;

public class KafkaOptions
{
    public ICollection<TopicDefinition> Topics { get; set; } = [];
    public ICollection<ConsumerDefinition> Consumers { get; set; } = [];
    public ICollection<ProducerDefinition> Producers { get; set; } = [];
    public string CorrelationHeaderKey { get; set; } = "x-correlation-id";
    public string TopicHeaderKey { get; set; } = "x-topic";
    
    public Func<IServiceProvider, object, string> Serializer { get; set; } = DefaultSerializers.SerializePayload;
    public Func<IServiceProvider, string, Type, object> Deserializer { get; set; } = DefaultSerializers.DeserializePayload;
}