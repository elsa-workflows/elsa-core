using System;
using Elsa.Features.Services;
using Elsa.Workflows.Sink.Features;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ModuleExtensions
{
    public static IModule UseWorkflowSink(this IModule module, Action<WorkflowSinkFeature>? configure = default)
    {
        module.Configure(configure);
        return module;
    }
}