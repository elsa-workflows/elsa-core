using Elsa.Framework.Shells.HostedServices;
using Elsa.Framework.Shells.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Framework.Shells.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddShells(this IServiceCollection services)
    {
        return services
            .AddSingleton<IShellFeatureTypesProvider, AssemblyShellFeatureTypesProvider>()
            .AddSingleton<ITenantShellFactory, TenantShellFactory>()
            .AddSingleton<ITenantShellHost, TenantShellHost>()
            .AddHostedService<CreateShellsHostedService>();
    }
    
    public static IServiceCollection Clone(this IServiceCollection services)
    {
        var clonedServices = new ServiceCollection();

        foreach (var serviceDescriptor in services)
        {
            clonedServices.Add(serviceDescriptor);
        }

        return clonedServices;
    }
}