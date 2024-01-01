namespace Elsa.Workflows.Models;

/// <summary>
/// Represents the outputs of an activity.
/// </summary>
/// <param name="ActivityId">The activity ID.</param>
/// <param name="ActivityName">The activity name.</param>
/// <param name="OutputNames">The output names.</param>
public record ActivityOutputs(string ActivityId, string ActivityName, ICollection<string> OutputNames);