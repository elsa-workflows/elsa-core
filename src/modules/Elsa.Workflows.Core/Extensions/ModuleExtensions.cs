using Elsa.Features.Services;
using Elsa.Workflows.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ModuleExtensions
{
    public static IModule UseWorkflows(this IModule configuration, Action<WorkflowsFeature>? configure = default)
    {
        configuration.Configure(configure);
        return configuration;
    }
}