namespace Elsa.Workflows.Activities.Flowchart.Models;

/// <summary>
/// Specifies the execution mode for flowchart activities.
/// </summary>
public enum FlowchartExecutionMode
{
    /// <summary>
    /// Use the default mode as specified by <see cref="Elsa.Workflows.Activities.Flowchart.Flowchart.UseTokenFlow"/>.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Use token-based flow logic.
    /// </summary>
    TokenBased = 1,

    /// <summary>
    /// Use counter-based flow logic (legacy mode).
    /// </summary>
    CounterBased = 2
}
