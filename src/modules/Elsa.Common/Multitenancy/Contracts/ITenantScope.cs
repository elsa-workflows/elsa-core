using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.Multitenancy;

public interface ITenantScope
{
    public IServiceScope ServiceScope { get; }
    IServiceProvider ServiceProvider { get; }
}