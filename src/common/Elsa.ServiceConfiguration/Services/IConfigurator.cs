using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ServiceConfiguration.Services;

public interface IConfigurator
{
    void ConfigureServices(IServiceCollection services);
    void ConfigureHostedServices(IServiceCollection services);
}