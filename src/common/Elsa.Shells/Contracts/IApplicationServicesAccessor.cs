using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Shells;

public interface IApplicationServicesAccessor
{
    IServiceCollection ApplicationServices { get; }
}