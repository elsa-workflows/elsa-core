using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Hosting.Management.HostedServices;
using Elsa.Hosting.Management.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Hosting.Management.Features;

/// <summary>
/// Installs and configures instance management features.
/// </summary>
public class InstanceManagementFeature : FeatureBase
{
    /// <inheritdoc />
    public InstanceManagementFeature(IModule module) : base(module)
    {
    }
    
    public Action<HeartbeatSettings> HeartbeatSettings { get; set; } = _ => { };

    public override void ConfigureHostedServices()
    {
        Module.ConfigureHostedService<InstanceHeartbeatService>();
        Module.ConfigureHostedService<InstanceHeartbeatMonitorService>();
    }

    public override void Apply()
    {
        Services.Configure(HeartbeatSettings);
    }
}