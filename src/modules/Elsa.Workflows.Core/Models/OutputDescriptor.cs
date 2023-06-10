using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Models;

/// <summary>
/// A descriptor of an activity's output property.
/// </summary>
public class OutputDescriptor : PropertyDescriptor
{
    /// <inheritdoc />
    [JsonConstructor]
    public OutputDescriptor()
    {
    }

    /// <inheritdoc />
    public OutputDescriptor(
        string name,
        string displayName,
        Type type,
        Func<IActivity, object?> valueGetter,
        string? description = default,
        bool? isBrowsable = default)
    {
        Name = name;
        DisplayName = displayName;
        Type = type;
        ValueGetter = valueGetter;
        Description = description;
        IsBrowsable = isBrowsable;
    }
}