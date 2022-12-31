using Elsa.Features.Services;
using Elsa.Workflows.Runtime.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ModuleExtensions
{
    public static IModule UseWorkflowRuntime(this IModule module, Action<WorkflowRuntimeFeature>? configure = default)
    {
        module.Configure(configure);
        return module;
    }
}