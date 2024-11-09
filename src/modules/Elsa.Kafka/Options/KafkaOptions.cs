using System.Text.Json;

namespace Elsa.Kafka;

public class KafkaOptions
{
    public ICollection<ConsumerDefinition> ConsumerDefinitions { get; set; } = [];
    public JsonSerializerOptions MessageSerializerOptions { get; set; } = new();
}