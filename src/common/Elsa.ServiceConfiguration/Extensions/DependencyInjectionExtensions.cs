using Elsa.ServiceConfiguration.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ServiceConfiguration.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceConfiguration ConfigureServices(this IServiceCollection services)
    {
        return new ServiceConfiguration.Implementations.ServiceConfiguration(services);
        //configure?.Invoke(serviceConfiguration);
        //serviceConfiguration.RunConfigurators();
        //return services;
    }
}