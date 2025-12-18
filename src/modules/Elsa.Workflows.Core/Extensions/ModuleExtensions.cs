using Elsa.Features.Services;
using Elsa.Workflows.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ModuleExtensions
{
    extension(IModule configuration)
    {
        public IModule UseWorkflows(Action<WorkflowsFeature>? configure = null)
        {
            configuration.Configure(configure);
            return configuration;
        }

        public IModule UseFlowchart(Action<FlowchartFeature>? configure = null)
        {
            configuration.Configure(configure);
            return configuration;
        }
    }
}