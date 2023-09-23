using Elsa.DropIns.Contracts;
using Elsa.DropIns.HostedServices;
using Elsa.DropIns.Options;
using Elsa.DropIns.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.DropIns.Extensions;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDropInMonitor(this IServiceCollection services, Action<DropInOptions>? configureOptions = default)
    {
        services.AddDropInCore(configureOptions);
        services.AddHostedService<DropInDirectoryMonitorHostedService>();
        
        return services;
    }
    
    public static IServiceCollection AddDropInInstaller(this IServiceCollection services, Action<DropInOptions>? configureOptions = default)
    {
        services.AddDropInCore(configureOptions);
        services.AddSingleton<IDropInInstaller, DropInInstaller>();
        
        return services;
    }
    
    public static IServiceCollection AddDropInCore(this IServiceCollection services, Action<DropInOptions>? configureOptions = default)
    {
        services.Configure(configureOptions ?? (_ => { }));

        return services;
    }
}