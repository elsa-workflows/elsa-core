using Elsa.ServiceConfiguration.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ServiceConfiguration.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection ConfigureServices(this IServiceCollection services, Action<IServiceConfiguration>? configure = default)
    {
        var configurator = new ServiceConfiguration.Implementations.ServiceConfiguration(services);
        configure?.Invoke(configurator);
        configurator.RunConfigurators();
        return services;
    }
}