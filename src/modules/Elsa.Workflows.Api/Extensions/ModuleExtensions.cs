using Elsa.Features.Services;
using Elsa.Workflows.Api.Features;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ModuleExtensions
{
    public static IModule UseWorkflowsApi(this IModule module, Action<WorkflowsApiFeature>? configure = default)
    {
        module.Configure(configure);
        return module;
    }
}