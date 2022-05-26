namespace Elsa.ServiceConfiguration.Services;

public interface IConfigurator
{
    IServiceConfiguration ServiceConfiguration { get; }
    void Configure();
    void ConfigureHostedServices();
    void Apply();
    
}