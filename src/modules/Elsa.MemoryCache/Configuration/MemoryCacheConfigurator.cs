using Elsa.ServiceConfiguration.Abstractions;
using Elsa.ServiceConfiguration.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.MemoryCache.Configuration;

public class MemoryCacheConfigurator : ConfiguratorBase
{
    public MemoryCacheConfigurator(IServiceConfiguration serviceConfiguration) : base(serviceConfiguration)
    {
    }

    public override void Configure()
    {
        Services.AddMemoryCache();
    }
}