using Elsa.ServiceConfiguration.Services;

namespace Elsa.ServiceConfiguration.Abstractions;

public abstract class ConfiguratorBase : IConfigurator
{
    protected ConfiguratorBase(IServiceConfiguration serviceConfiguration)
    {
        ServiceConfiguration = serviceConfiguration;
    }
    
    public IServiceConfiguration ServiceConfiguration { get; set; }

    public virtual void ConfigureServices(IServiceConfiguration serviceConfiguration)
    {
    }

    public virtual void ConfigureHostedServices(IServiceConfiguration serviceConfiguration)
    {
    }
}