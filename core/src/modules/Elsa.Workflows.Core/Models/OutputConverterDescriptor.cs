using System.Text.Json;

namespace Elsa.Workflows.Models;

/// <summary>
/// Describes an output converter without exposing its implementation.
/// </summary>
public sealed record OutputConverterDescriptor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutputConverterDescriptor"/> class.
    /// </summary>
    public OutputConverterDescriptor(
        string id,
        Type sourceType,
        Type resultType,
        string displayName,
        string? description = null,
        JsonElement? settingsSchema = null)
    {
        Id = id;
        SourceType = sourceType;
        ResultType = resultType;
        DisplayName = displayName;
        Description = description;
        SettingsSchema = settingsSchema?.Clone();
    }

    /// <summary>The stable, ordinal case-sensitive converter ID.</summary>
    public string Id { get; }

    /// <summary>The declared source type accepted by the converter.</summary>
    public Type SourceType { get; }

    /// <summary>The declared result type produced by the converter.</summary>
    public Type ResultType { get; }

    /// <summary>The display name exposed through discovery.</summary>
    public string DisplayName { get; }

    /// <summary>The optional description exposed through discovery.</summary>
    public string? Description { get; }

    /// <summary>An immutable optional JSON Schema for per-binding settings.</summary>
    public JsonElement? SettingsSchema { get; }
}
