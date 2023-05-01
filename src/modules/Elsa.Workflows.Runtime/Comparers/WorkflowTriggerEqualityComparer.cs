using System.Text.Json;
using Elsa.Expressions.Services;
using Elsa.Workflows.Core.Serialization.Converters;
using Elsa.Workflows.Runtime.Entities;

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
            PropertyNameCaseInsensitive = true
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
        var input = new
        {
            storedTrigger.Payload,
            storedTrigger.Name,
            storedTrigger.ActivityId,
            storedTrigger.WorkflowDefinitionId
        };
        return JsonSerializer.Serialize(input, _settings);
    }
}