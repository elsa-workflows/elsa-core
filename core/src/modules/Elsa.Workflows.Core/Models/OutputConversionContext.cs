using System.Text.Json;

namespace Elsa.Workflows.Models;

/// <summary>
/// Provides the immutable values available to an output converter.
/// </summary>
public sealed record OutputConversionContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutputConversionContext"/> class.
    /// </summary>
    public OutputConversionContext(object value, Type sourceType, Type destinationType, JsonElement? settings)
    {
        Value = value;
        SourceType = sourceType;
        DestinationType = destinationType;
        Settings = settings?.Clone();
    }

    /// <summary>The non-null native activity output.</summary>
    public object Value { get; }

    /// <summary>The activity output's declared type.</summary>
    public Type SourceType { get; }

    /// <summary>The binding destination's declared type.</summary>
    public Type DestinationType { get; }

    /// <summary>A clone of the optional per-binding JSON settings.</summary>
    public JsonElement? Settings { get; }
}
