using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Framework.Shells;

public interface IApplicationServicesAccessor
{
    IServiceCollection ApplicationServices { get; }
}