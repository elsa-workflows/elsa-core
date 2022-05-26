namespace Elsa.ServiceConfiguration.Services;

public interface IConfigurator
{
    IServiceConfiguration ServiceConfiguration { get; }
    void ConfigureServices(IServiceConfiguration serviceConfiguration);
    void ConfigureHostedServices(IServiceConfiguration serviceConfiguration);
}