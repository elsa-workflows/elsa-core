using Elsa.Diagnostics.Contracts;
using Elsa.Diagnostics.Logging;
using Elsa.Diagnostics.Options;
using Elsa.Diagnostics.Providers.InMemory;
using Elsa.Diagnostics.RealTime;
using Elsa.Diagnostics.Services;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Elsa.Diagnostics.Features;

public class ServerLogStreamingFeature(IModule module) : FeatureBase(module)
{
    public Action<ServerLogStreamingOptions>? ConfigureOptions { get; set; }
    
    public override void Configure()
    {
        Module.AddFastEndpointsAssembly<ServerLogStreamingFeature>();
    }
    
    public override void Apply()
    {
        if (ConfigureOptions != null)
            Services.Configure(ConfigureOptions);
        
        Services.AddSignalR();
        Services.AddOptions<ServerLogStreamingOptions>();
        Services.TryAddSingleton<IServerLogSourceRegistry, ServerLogSourceRegistry>();
        Services.TryAddSingleton<IServerLogRedactor, ServerLogRedactor>();
        Services.TryAddSingleton<IServerLogProvider, InMemoryServerLogProvider>();
        Services.TryAddSingleton<ServerLogSubscriptionManager>();
        Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, ServerLogLoggerProvider>());
        Module.AddFastEndpointsFromModule();
    }
}
