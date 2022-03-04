using Elsa.Runtime.Contracts;

namespace Elsa.Runtime.Stimuli;

/// <summary>
/// Represents a simple stimulus that targets a specific activity type and optionally a corresponding hash to use as a bookmark lookup.
/// </summary>
public record StandardStimulus(string ActivityTypeName, string? Hash = default, IReadOnlyDictionary<string, object?>? Input = default) : IStimulus;