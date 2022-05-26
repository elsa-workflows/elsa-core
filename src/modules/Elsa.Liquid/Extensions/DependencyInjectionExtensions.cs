using Elsa.Liquid.Configuration;
using Elsa.Liquid.Implementations;
using Elsa.Liquid.Options;
using Elsa.Liquid.Services;
using Elsa.ServiceConfiguration.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Liquid.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceConfiguration UseLiquid(this IServiceConfiguration configuration, Action<LiquidConfigurator>? configure = default)
    {
        configuration.Configure(configure);
        return configuration;
    }

    public static IServiceCollection AddLiquidFilter<T>(this IServiceCollection services, string name) where T : class, ILiquidFilter
    {
        services.Configure<FluidOptions>(options => options.FilterRegistrations[name] = typeof(T));
        services.AddScoped<T>();
        return services;
    }

    public static IServiceCollection RegisterLiquidTag(this IServiceCollection services, Action<LiquidParser> configure)
    {
        services.Configure<FluidOptions>(options => options.ParserConfiguration.Add(configure));
        return services;
    }

    /// <summary>
    /// Enables access to .NET configuration via the Configuration keyword.
    /// Do not enable this option if you execute user supplied (or otherwise untrusted) workflows.
    /// </summary>
    public static IServiceCollection EnableLiquidConfigurationAccess(this IServiceCollection services)
    {
        services.Configure<FluidOptions>(options => options.AllowConfigurationAccess = true);
        return services;
    }
}