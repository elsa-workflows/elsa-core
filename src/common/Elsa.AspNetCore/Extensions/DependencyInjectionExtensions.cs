using Elsa.AspNetCore.Configuration;
using Elsa.ServiceConfiguration.Services;

namespace Elsa.AspNetCore.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceConfiguration UseMvc(this IServiceConfiguration configuration, Action<MvcConfigurator>? configure = default)
    {
        configuration.Configure(configure);
        return configuration;
    }
}