using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Tenants;

public interface IApplicationServicesAccessor
{
    IServiceCollection ApplicationServices { get; }
}