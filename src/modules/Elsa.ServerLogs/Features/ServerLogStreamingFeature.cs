using Elsa.ServerLogs.Extensions;
using Elsa.ServerLogs.Options;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;

namespace Elsa.ServerLogs.Features;

public class ServerLogStreamingFeature(IModule module) : FeatureBase(module)
{
    public Action<ServerLogStreamingOptions>? ConfigureOptions { get; set; }
    
    public override void Configure()
    {
        Module.AddFastEndpointsAssembly<ServerLogStreamingFeature>();
    }
    
    public override void Apply()
    {
        Services.AddServerLogStreamingServices(ConfigureOptions);
        Module.AddFastEndpointsFromModule();
    }
}
