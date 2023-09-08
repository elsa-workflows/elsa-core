namespace Elsa.Workflows.Core.Activities.Flowchart.Models;

/// <summary>
/// Represents a list of outcomes that can be send when completing an activity. This information is used by <see cref="Activities.Flowchart"/>.
/// </summary>
/// <param name="Names">A list of outcome names.</param>
public record Outcomes(params string[] Names);