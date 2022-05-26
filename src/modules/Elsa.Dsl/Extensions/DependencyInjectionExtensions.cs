using System;
using Elsa.Dsl.Configuration;
using Elsa.ServiceConfiguration.Services;

namespace Elsa.Dsl.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceConfiguration UseDsl(this IServiceConfiguration configuration, Action<DslConfigurator>? configure = default)
    {
        configuration.Configure(configure);
        return configuration;
    }
}