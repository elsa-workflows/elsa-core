using System.Text.Json;

namespace Elsa.Api.Client.Resources.OutputConverters.Models;

/// <summary>
/// Describes an output converter that can be selected by a workflow author.
/// </summary>
public record OutputConverterDescriptor
{
    /// <summary>
    /// The stable public converter identifier.
    /// </summary>
    public string Id { get; init; } = null!;

    /// <summary>
    /// The declared source type name supported by the converter.
    /// </summary>
    public string SourceTypeName { get; init; } = null!;

    /// <summary>
    /// The declared result type name produced by the converter.
    /// </summary>
    public string ResultTypeName { get; init; } = null!;

    /// <summary>
    /// The display name for the converter.
    /// </summary>
    public string DisplayName { get; init; } = null!;

    /// <summary>
    /// The optional converter description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The optional JSON schema for converter settings.
    /// </summary>
    public JsonElement? SettingsSchema { get; init; }
}
