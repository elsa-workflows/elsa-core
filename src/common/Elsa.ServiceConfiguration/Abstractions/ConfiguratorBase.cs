using Elsa.ServiceConfiguration.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ServiceConfiguration.Abstractions;

public abstract class ConfiguratorBase : IConfigurator
{
    protected ConfiguratorBase(IServiceConfiguration serviceConfiguration)
    {
        ServiceConfiguration = serviceConfiguration;
    }

    public IServiceConfiguration ServiceConfiguration { get; }
    public IServiceCollection Services => ServiceConfiguration.Services;

    public virtual void Configure()
    {
    }

    public virtual void ConfigureHostedServices()
    {
    }

    public virtual void Apply()
    {
    }
}