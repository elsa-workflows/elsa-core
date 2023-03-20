using Elsa.Features.Services;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Features;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ModuleExtensions
{
    public static IModule UseWorkflows(this IModule configuration, Action<WorkflowsFeature>? configure = default)
    {
        configuration.Configure(configure);
        return configuration;
    }

    public static IServiceCollection AddStorageDriver<T>(this IServiceCollection services) where T : class, IStorageDriver
    {
        return services.AddSingleton<IStorageDriver, T>();
    }
}