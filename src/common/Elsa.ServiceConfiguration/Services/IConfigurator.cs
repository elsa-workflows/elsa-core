namespace Elsa.ServiceConfiguration.Services;

public interface IConfigurator
{
    IServiceConfiguration ServiceConfiguration { get; }
    void ConfigureServices();
    void ConfigureHostedServices();
}