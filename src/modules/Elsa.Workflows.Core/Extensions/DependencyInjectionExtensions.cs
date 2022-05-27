using Elsa.Features.Services;
using Elsa.Workflows.Core.Features;

namespace Elsa.Workflows.Core;

public static class DependencyInjectionExtensions
{
    public static IModule UseWorkflows(this IModule configuration, Action<WorkflowsFeature>? configure = default)
    {
        configuration.Configure(configure);
        return configuration;
    }
}