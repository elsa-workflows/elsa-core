namespace Elsa.Platform.Integration.Services;

internal sealed class PlatformRecipeServiceProvider(
    IServiceProvider inner,
    IReadOnlyDictionary<Type, object> services) : IServiceProvider
{
    public object? GetService(Type serviceType) =>
        services.TryGetValue(serviceType, out var service) ? service : inner.GetService(serviceType);
}
