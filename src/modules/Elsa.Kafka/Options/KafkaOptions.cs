using System.Text.Json;

namespace Elsa.Kafka;

public class KafkaOptions
{
    public ICollection<TopicDefinition> Topics { get; set; } = [];
    public ICollection<ConsumerDefinition> Consumers { get; set; } = [];
    public ICollection<ProducerDefinition> Producers { get; set; } = [];
    public JsonSerializerOptions MessageSerializerOptions { get; set; } = new();
}