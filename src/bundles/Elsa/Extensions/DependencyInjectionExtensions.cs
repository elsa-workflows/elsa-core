using System;
using Elsa.Features;
using Elsa.Features.Extensions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddElsa(this IServiceCollection services, Action<IModule>? configure = default)
    {
        var serviceConfiguration = services.ConfigureElsa();
        configure?.Invoke(serviceConfiguration);
        serviceConfiguration.Apply();
        return services;
    }

    public static IModule ConfigureElsa(this IServiceCollection services)
    {
        var module = services.CreateModule();
        module.Configure<ElsaFeature>();

        return module;
    }
}