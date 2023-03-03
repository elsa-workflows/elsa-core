using Elsa.Liquid.Contracts;
using Elsa.Liquid.Options;
using Elsa.Liquid.Services;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Register a custom Liquid filter.
    /// </summary>
    public static IServiceCollection AddLiquidFilter<T>(this IServiceCollection services, string name) where T : class, ILiquidFilter
    {
        services.Configure<FluidOptions>(options => options.FilterRegistrations[name] = typeof(T));
        services.AddScoped<T>();
        return services;
    }

    /// <summary>
    /// Register a custom Liquid tag.
    /// </summary>
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