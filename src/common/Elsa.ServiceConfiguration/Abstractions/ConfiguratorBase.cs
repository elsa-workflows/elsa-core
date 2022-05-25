using Elsa.ServiceConfiguration.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ServiceConfiguration.Abstractions;

public abstract class ConfiguratorBase : IConfigurator
{
    public virtual void ConfigureServices(IServiceCollection services)
    {
    }

    public virtual void ConfigureHostedServices(IServiceCollection services)
    {
    }
}