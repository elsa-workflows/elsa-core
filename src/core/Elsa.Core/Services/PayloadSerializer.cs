using System.Text.Json;
using Elsa.Contracts;

namespace Elsa.Services;

public class PayloadSerializer : IPayloadSerializer
{
    private readonly JsonSerializerOptions _settings;

    public PayloadSerializer()
    {
        _settings = new JsonSerializerOptions
        {
            // Enables serialization of ValueTuples, which use fields instead of properties.
            IncludeFields = true
        };
    }

    public T Deserialize<T>(string json) where T : notnull => JsonSerializer.Deserialize<T>(json, _settings)!;
    public string Serialize<T>(T payload) where T : notnull => JsonSerializer.Serialize(payload, payload.GetType(), _settings);
}