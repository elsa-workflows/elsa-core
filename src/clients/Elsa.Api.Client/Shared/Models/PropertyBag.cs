using System.Text.Json.Serialization;

namespace Elsa.Api.Client.Shared.Models;

/// A dictionary of values that is skipped by polymorphic serialization.
public class PropertyBag : Dictionary<string, object>
{
    /// <inheritdoc />
    [JsonConstructor]
    public PropertyBag() : base(StringComparer.OrdinalIgnoreCase)
    {
    }

    /// <inheritdoc />
    public PropertyBag(IDictionary<string, object> dictionary) : this()
    {
        foreach (var kvp in dictionary) 
            Add(kvp.Key, kvp.Value);
    }
}