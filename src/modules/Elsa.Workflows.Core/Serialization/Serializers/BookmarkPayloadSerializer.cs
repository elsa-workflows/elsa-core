using System.Text.Json;
using Elsa.Workflows.Serialization.Converters;
using Elsa.Workflows.Serialization.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Serialization.Serializers;

/// <inheritdoc />
public class BookmarkPayloadSerializer : IBookmarkPayloadSerializer
{
    private readonly JsonSerializerOptions _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="BookmarkPayloadSerializer"/> class.
    /// </summary>
    public BookmarkPayloadSerializer()
        : this(Microsoft.Extensions.Options.Options.Create(new WorkflowJsonOptions()))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BookmarkPayloadSerializer"/> class.
    /// </summary>
    public BookmarkPayloadSerializer(IOptions<WorkflowJsonOptions> workflowJsonOptions)
    {
        _settings = new JsonSerializerOptions
        {
            // Enables serialization of ValueTuples, which use fields instead of properties.
            IncludeFields = true,
            PropertyNameCaseInsensitive = true,
        };

        _settings.Converters.Add(new TypeJsonConverter(workflowJsonOptions));
        _settings.Converters.Add(new FuncExpressionValueConverter());
    }

    /// <inheritdoc />
    public T Deserialize<T>(string json)
        where T : notnull => JsonSerializer.Deserialize<T>(json, _settings)!;

    /// <inheritdoc />
    public object Deserialize(string json, Type type) => JsonSerializer.Deserialize(json, type, _settings)!;

    /// <inheritdoc />
    public string Serialize<T>(T payload)
        where T : notnull => JsonSerializer.Serialize(payload, payload.GetType(), _settings);
}
