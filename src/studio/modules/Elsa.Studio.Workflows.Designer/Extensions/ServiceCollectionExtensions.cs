using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Designer.Contracts;
using Elsa.Studio.Workflows.Designer.Interop;
using Elsa.Studio.Workflows.Designer.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Workflows.Designer.Extensions;

/// <summary>
/// Provides extension methods for service collection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the workflows designer.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The result of the operation.</returns>
    public static IServiceCollection AddWorkflowsDesigner(this IServiceCollection services)
    {
        return services
            .AddScoped<IFeature, Feature>()
            .AddScoped<IMapperFactory, MapperFactory>()
            .AddScoped<StateMachineValidator>()
            .AddScoped<IStateMachineMapper, StateMachineMapper>()
            .AddScoped<DesignerJsInterop>()
            .AddScoped<ReactFlowJsInterop>();
    }
}
