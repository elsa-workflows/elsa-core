using Elsa.Connections.Features;
using Elsa.Connections.Middleware;
using Elsa.Features.Services;
using Elsa.Workflows;
using Elsa.Workflows.Pipelines.ActivityExecution;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Extends <see cref="IModule"/> with methods to install Semantic Kernel API endpoints.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Installs the Semantic Kernel API feature.
    /// </summary>
    public static IModule UseConnections(this IModule module, Action<ConnectionsFeatures>? configure = null)
    {
        return module.Use(configure);
    }
}

/// <summary>
/// Adds extension methods to <see cref="ConnectionMiddleware"/>.
/// </summary>
public static class ConnectionMiddlewareExtensions
{
    /// <summary>
    /// Installs the <see cref="ConnectionMiddleware"/> component in the activity execution pipeline.
    /// </summary>
    public static IActivityExecutionPipelineBuilder UseConnectionMiddleware(this IActivityExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.UseMiddleware<ConnectionMiddleware>();
}
