namespace Elsa.ServiceConfiguration.Services;

public interface IConfigurator
{
    void ConfigureServices(IServiceConfiguration serviceConfiguration);
    void ConfigureHostedServices(IServiceConfiguration serviceConfiguration);
}