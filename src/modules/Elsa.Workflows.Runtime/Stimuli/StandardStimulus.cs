using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Runtime.Stimuli;

/// <summary>
/// Represents a simple stimulus that targets a specific activity type and optionally a corresponding hash to use as a bookmark lookup.
/// </summary>
public record StandardStimulus(string ActivityTypeName, string? Hash = default, IDictionary<string, object>? Input = default, string? CorrelationId = default) : IStimulus;