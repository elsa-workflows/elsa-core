using System.Text.Json;
using Elsa.Expressions.Contracts;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Serialization.Converters;

namespace Elsa.Workflows.Core.Services;

/// <inheritdoc />
public class BookmarkHasher : IBookmarkHasher
{
    private readonly IHasher _hasher;
    private readonly JsonSerializerOptions _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="BookmarkHasher"/> class.
    /// </summary>
    public BookmarkHasher(IHasher hasher, IWellKnownTypeRegistry wellKnownTypeRegistry)
    {
        _hasher = hasher;
        
        _settings = new JsonSerializerOptions
        {
            // Enables serialization of ValueTuples, which use fields instead of properties.
            IncludeFields = true,
            PropertyNameCaseInsensitive = true
        };
        
        _settings.Converters.Add(new TypeJsonConverter(wellKnownTypeRegistry));
        _settings.Converters.Add(new ExcludeFromHashConverterFactory());
    }

    /// <inheritdoc />
    public string Hash(string activityTypeName, object? payload)
    {
        var json = payload != null ? Serialize(payload) : null;
        var input = $"{activityTypeName}{(!string.IsNullOrWhiteSpace(json) ? ":" + json : "")}";
        var hash = _hasher.Hash(input);

        return hash;
    }

    private string Serialize(object payload) => JsonSerializer.Serialize(payload, payload.GetType(), _settings);
}