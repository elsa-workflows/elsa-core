using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ServiceConfiguration.Services;

public interface IServiceConfiguration
{
    IServiceCollection Services { get; }
    T Configure<T>(Action<T>? configure = default) where T : class, IConfigurator;
    T Configure<T>(Func<IServiceConfiguration, T> factory, Action<T>? configure = default) where T : class, IConfigurator;
    IServiceConfiguration ConfigureHostedService<T>(int priority = 0);
    void Apply();
}