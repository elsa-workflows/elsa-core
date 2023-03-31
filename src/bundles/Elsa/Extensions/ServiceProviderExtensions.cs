using Elsa.Workflows.Core.Contracts;
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
    public static IServiceProvider ConfigureDefaultActivityExecutionPipeline(this IServiceProvider services, Action<IActivityExecutionPipelineBuilder> setup)
    {
        var pipeline = services.GetRequiredService<IActivityExecutionPipeline>();
        pipeline.Setup(setup);
        return services;
    }
    
}