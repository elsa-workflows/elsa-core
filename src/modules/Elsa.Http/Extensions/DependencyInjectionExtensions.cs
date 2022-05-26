using System;
using Elsa.Http.Configuration;
using Elsa.ServiceConfiguration.Services;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IServiceConfiguration UseHttp(this IServiceConfiguration configuration, Action<HttpConfigurator>? configure = default)
    {
        configuration.Configure(configure);
        return configuration;
    }
}