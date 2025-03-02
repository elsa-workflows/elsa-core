using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Framework.Shells;

public interface IRootServicesAccessor
{
    IServiceCollection RootServices { get; }
}