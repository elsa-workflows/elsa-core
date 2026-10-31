using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Options;

namespace Elsa.Workflows.Activities.Flowchart.Extensions;

/// <summary>
/// Extension methods for <see cref="RunWorkflowOptions"/> to configure flowchart execution mode.
/// </summary>
public static class RunWorkflowOptionsExtensions
{
    extension(RunWorkflowOptions options)
    {
        /// <summary>
        /// Sets the flowchart execution mode to token-based.
        /// </summary>
        public RunWorkflowOptions WithTokenBasedFlowchart()
        {
            return options.WithFlowchartExecutionMode(FlowchartExecutionMode.TokenBased);
        }

        /// <summary>
        /// Sets the flowchart execution mode to counter-based (legacy mode).
        /// </summary>
        public RunWorkflowOptions WithCounterBasedFlowchart()
        {
            return options.WithFlowchartExecutionMode(FlowchartExecutionMode.CounterBased);
        }

        /// <summary>
        /// Sets the flowchart execution mode.
        /// </summary>
        public RunWorkflowOptions WithFlowchartExecutionMode(FlowchartExecutionMode mode)
        {
            options.Properties ??= new Dictionary<string, object>();
            options.Properties[Activities.Flowchart.ExecutionModePropertyKey] = mode;
            return options;
        }
    }
}
