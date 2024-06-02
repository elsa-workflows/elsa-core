using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Tenants.Services;

public class DefaultApplicationServicesAccessor(IServiceCollection serviceCollection) : IApplicationServicesAccessor
{
    public IServiceCollection ApplicationServices { get; } = serviceCollection;
}