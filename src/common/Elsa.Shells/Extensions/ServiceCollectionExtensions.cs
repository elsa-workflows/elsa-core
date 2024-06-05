using Elsa.Shells.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Shells.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddShellFeatures(this IServiceCollection services)
    {
        services.AddSingleton<IShellFeatureTypesProvider, AssemblyShellFeatureTypesProvider>();
        return services;
    }
}