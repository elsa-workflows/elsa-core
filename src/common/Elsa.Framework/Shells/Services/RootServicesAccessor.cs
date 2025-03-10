using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Framework.Shells.Services;

public class RootServicesAccessor(IServiceCollection serviceCollection) : IRootServicesAccessor
{
    public IServiceCollection RootServices { get; } = serviceCollection;
}