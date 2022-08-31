using Elsa.Workflows.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Extensions;

/// <summary>
/// Provides extension methods to the <see cref="IServiceProvider"/>.
/// </summary>
public static class ServiceProviderExtensions
{
    /// <summary>
    /// Configure the default activity execution pipeline.
    /// </summary>
    public static IServiceProvider ConfigureDefaultActivityExecutionPipeline(this IServiceProvider services, Action<IActivityExecutionBuilder> setup)
    {
        var pipeline = services.GetRequiredService<IActivityExecutionPipeline>();
        pipeline.Setup(setup);
        return services;
    }
        
    /// <summary>
    /// Configure the default workflow execution pipeline.
    /// </summary>
    public static IServiceProvider ConfigureDefaultWorkflowExecutionPipeline(this IServiceProvider services, Action<IWorkflowExecutionBuilder> setup)
    {
        var pipeline = services.GetRequiredService<IWorkflowExecutionPipeline>();
        pipeline.Setup(setup);
        return services;
    }
}