using System.Text.Json;

namespace Elsa.Kafka;

public class KafkaOptions
{
    public ICollection<ConsumerDefinition> ConsumerConfigs { get; set; } = [];
    public JsonSerializerOptions MessageSerializerOptions { get; set; } = new();
}