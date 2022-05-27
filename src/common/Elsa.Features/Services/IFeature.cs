namespace Elsa.Features.Services;

public interface IFeature
{
    IModule Module { get; }
    void Configure();
    void ConfigureHostedServices();
    void Apply();
    
}