using System.Text.Json;
using System.Text.Json.Serialization;
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
        _settings = new JsonSerializerOptions
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
            
        // Serialize to camelCase JSON — this normalizes both:
        // - CLR objects (whose PascalCase properties get converted to camelCase)
        // - JsonElement values (whose camelCase keys are preserved as-is)
        return JsonSerializer.Serialize(payload, _settings);
    }
}