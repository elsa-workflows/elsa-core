using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Activities.Flowchart.Options;
using Elsa.Workflows.Features;

namespace Elsa.Workflows.Activities.Flowchart.Extensions;

/// <summary>
/// Extension methods for <see cref="FlowchartFeature"/>.
/// </summary>
public static class FlowchartFeatureExtensions
{
    extension(FlowchartFeature feature)
    {
        /// <summary>
        /// Configures the flowchart options.
        /// </summary>
        public FlowchartFeature ConfigureFlowchart(Action<FlowchartOptions> configure)
        {
            feature.FlowchartOptionsConfigurator = configure;
            return feature;
        }

        /// <summary>
        /// Sets the default execution mode for flowcharts to token-based.
        /// </summary>
        public FlowchartFeature UseTokenBasedExecution()
        {
            return feature.ConfigureFlowchart(options => options.DefaultExecutionMode = FlowchartExecutionMode.TokenBased);
        }

        /// <summary>
        /// Sets the default execution mode for flowcharts to counter-based (legacy mode).
        /// </summary>
        public FlowchartFeature UseCounterBasedExecution()
        {
            return feature.ConfigureFlowchart(options => options.DefaultExecutionMode = FlowchartExecutionMode.CounterBased);
        }
        
        /// <summary>
        /// Sets the default execution mode for flowcharts to token-based.
        /// </summary>
        public FlowchartFeature UseExecution(FlowchartExecutionMode mode)
        {
            return feature.ConfigureFlowchart(options => options.DefaultExecutionMode = mode);
        }
    }
}
