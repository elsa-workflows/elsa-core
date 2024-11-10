using System.Text.Json;

namespace Elsa.Kafka;

public class KafkaOptions
{
    public ICollection<TopicDefinition> TopicDefinitions { get; set; } = [];
    public ICollection<ConsumerDefinition> ConsumerDefinitions { get; set; } = [];
    public ICollection<ProducerDefinition> ProducerDefinitions { get; set; } = [];
    public JsonSerializerOptions MessageSerializerOptions { get; set; } = new();
}