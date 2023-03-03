using System.Text.Json;
using Elsa.Expressions.Contracts;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Serialization.Converters;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Implementations;

public class BookmarkPayloadSerializer : IBookmarkPayloadSerializer
{
    private readonly JsonSerializerOptions _settings;

    public BookmarkPayloadSerializer(IWellKnownTypeRegistry wellKnownTypeRegistry)
    {
        _settings = new JsonSerializerOptions
        {
            // Enables serialization of ValueTuples, which use fields instead of properties.
            IncludeFields = true,
            PropertyNameCaseInsensitive = true
        };
        
        _settings.Converters.Add(new TypeJsonConverter(wellKnownTypeRegistry));
    }

    public T Deserialize<T>(string json) where T : notnull => JsonSerializer.Deserialize<T>(json, _settings)!;
    public object Deserialize(string json, Type type) => JsonSerializer.Deserialize(json, type, _settings)!;
    public string Serialize<T>(T payload) where T : notnull => JsonSerializer.Serialize(payload, payload.GetType(), _settings);
}