using Avro.Specific;
using Avro;

namespace Trimble.Elsa.Activities.Kafka;

public class AvroDataPropertyMessage : ISpecificRecord
{
    public static readonly Schema SCHEMA = Schema.Parse(@"
    {
        ""type"": ""record"",
        ""name"": ""AvroDataPropertyMessage"",
        ""fields"": [
            { ""name"": ""CorrelationId"", ""type"": ""string"" },
            { ""name"": ""Data"", ""type"": { ""type"": ""map"", ""values"": ""string"" } }
        ]
    }");

    public Schema Schema => SCHEMA;

    public object Get(int fieldPos)
    {
        return fieldPos switch
        {
            0 => CorrelationId,
            1 => Data,
            _ => throw new AvroRuntimeException("Bad index " + fieldPos),
        };
    }

    public void Put(int fieldPos, object value)
    {
        switch (fieldPos)
        {
            case 0: CorrelationId = value.ToString() ?? string.Empty; break;
            case 1: Data = (Dictionary<string, string>)value; break;
            default: throw new AvroRuntimeException("Bad index " + fieldPos);
        }
    }

    public string CorrelationId { get; set; } = string.Empty;
    public Dictionary<string, string> Data { get; set; } = new Dictionary<string, string>();
}
