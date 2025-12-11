using Elsa.Workflows.Activities.Flowchart.Models;

namespace Elsa.Workflows.Activities.Flowchart.Options;

/// <summary>
/// Options for configuring flowchart execution behavior.
/// </summary>
public class FlowchartOptions
{
    /// <summary>
    /// Gets or sets the default execution mode for flowcharts when not explicitly specified.
    /// Defaults to <see cref="FlowchartExecutionMode.TokenBased"/>.
    /// </summary>
    public FlowchartExecutionMode DefaultExecutionMode { get; set; } = FlowchartExecutionMode.TokenBased;
}
