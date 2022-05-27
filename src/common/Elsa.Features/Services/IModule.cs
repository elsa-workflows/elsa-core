using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Features.Services;

public interface IModule
{
    IServiceCollection Services { get; }
    T Configure<T>(Action<T>? configure = default) where T : class, IFeature;
    T Configure<T>(Func<IModule, T> factory, Action<T>? configure = default) where T : class, IFeature;
    IModule ConfigureHostedService<T>(int priority = 0);
    void Apply();
}