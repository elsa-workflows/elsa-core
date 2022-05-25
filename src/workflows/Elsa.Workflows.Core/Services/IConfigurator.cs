using Elsa.Options;

namespace Elsa.Services;

public interface IConfigurator
{
    void ConfigureServices(ElsaOptionsConfigurator configurator);
    void ConfigureHostedServices(ElsaOptionsConfigurator configurator);
}

public abstract class ConfiguratorBase : IConfigurator
{
    public virtual void ConfigureServices(ElsaOptionsConfigurator configurator)
    {
    }

    public virtual void ConfigureHostedServices(ElsaOptionsConfigurator configurator)
    {
    }
}