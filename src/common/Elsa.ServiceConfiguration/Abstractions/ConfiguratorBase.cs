using Elsa.ServiceConfiguration.Services;

namespace Elsa.ServiceConfiguration.Abstractions;

public abstract class ConfiguratorBase : IConfigurator
{
    public virtual void ConfigureServices(IServiceConfiguration serviceConfiguration)
    {
    }

    public virtual void ConfigureHostedServices(IServiceConfiguration serviceConfiguration)
    {
    }
}