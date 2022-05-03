using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Services;

public interface IConfigurator
{
    void ConfigureServices(IServiceCollection services);
    void ConfigureHostedServices(IServiceCollection services);
}

public abstract class ConfiguratorBase : IConfigurator
{
    public virtual void ConfigureServices(IServiceCollection services)
    {
    }

    public virtual void ConfigureHostedServices(IServiceCollection services)
    {
    }
}