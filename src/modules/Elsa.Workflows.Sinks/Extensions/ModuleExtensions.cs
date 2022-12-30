using System;
using Elsa.Features.Services;
using Elsa.Workflows.Sinks.Features;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ModuleExtensions
{
    public static IModule UseWorkflowSinks(this IModule module, Action<WorkflowSinkFeature>? configure = default)
    {
        module.Configure(configure);
        return module;
    }
}