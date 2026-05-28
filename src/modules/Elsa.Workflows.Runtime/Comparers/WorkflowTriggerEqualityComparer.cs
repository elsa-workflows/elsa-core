using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Elsa.Expressions.Services;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Serialization.Converters;

namespace Elsa.Workflows.Runtime.Comparers;

/// <summary>
/// Compares two <see cref="StoredTrigger"/> instances for equality.
/// </summary>
public class WorkflowTriggerEqualityComparer : IEqualityComparer<StoredTrigger>
{
    private readonly JsonSerializerOptions _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowTriggerEqualityComparer"/> class.
    /// </summary>
    public WorkflowTriggerEqualityComparer()
    {
        _settings = new()
        {
            // Enables serialization of ValueTuples, which use fields instead of properties.
            IncludeFields = true,
            PropertyNameCaseInsensitive = true,
            // Use camelCase to match IPayloadSerializer, which stores payloads using camelCase.
            // Without this, fresh payloads (typed CLR objects) serialize with PascalCase while
            // DB-loaded payloads (JsonElement) preserve their stored camelCase keys, causing
            // the diff to always report all triggers as changed.
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        // Mirror the converters used by IPayloadSerializer so that enum, TimeSpan, and
        // polymorphic object properties serialize identically to their stored representation.
        _settings.Converters.Add(new JsonStringEnumConverter());
        _settings.Converters.Add(JsonMetadataServices.TimeSpanConverter);
        _settings.Converters.Add(new PolymorphicObjectConverterFactory());
        _settings.Converters.Add(new TypeJsonConverter(WellKnownTypeRegistry.CreateDefault()));
    }

    /// <inheritdoc />
    public bool Equals(StoredTrigger? x, StoredTrigger? y)
    {
        var xJson = x != null ? Serialize(x) : null;
        var yJson = y != null ? Serialize(y) : null;
        return xJson == yJson;
    }

    /// <inheritdoc />
    public int GetHashCode(StoredTrigger obj)
    {
        var json = Serialize(obj);
        return json.GetHashCode();
    }

    private string Serialize(StoredTrigger storedTrigger)
    {
        // Normalize the payload to a canonical JSON string so that both typed CLR objects
        // and JsonElement instances (from DB round-trips) produce identical output.
        var normalizedPayload = NormalizePayload(storedTrigger.Payload);

        var input = new
        {
            Payload = normalizedPayload,
            storedTrigger.Name,
            storedTrigger.ActivityId,
            storedTrigger.WorkflowDefinitionId,
            storedTrigger.WorkflowDefinitionVersionId,
            storedTrigger.Hash
        };
        return JsonSerializer.Serialize(input, _settings);
    }

    /// <summary>
    /// Normalizes a payload to a canonical JSON string representation.
    /// This ensures that typed CLR objects and JsonElements (which preserve their original
    /// property name casing from DB storage) produce identical output when compared.
    /// </summary>
    private string? NormalizePayload(object? payload)
    {
        if (payload == null)
            return null;

        // Serialize using the concrete runtime type rather than object, so that
        // PolymorphicObjectConverterFactory (which only handles typeof(object)) does not
        // inject a "_type" discriminator field into CLR payloads. Without this,
        // fresh CLR payloads produce {"path":"...","_type":"..."} while DB-loaded
        // JsonElement payloads produce {"path":"..."}, and the two never compare equal.
        return JsonSerializer.Serialize(payload, payload.GetType(), _settings);
    }
}

