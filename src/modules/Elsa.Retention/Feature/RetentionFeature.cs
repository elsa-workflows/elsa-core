using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Retention.HostedServices;
using Elsa.Retention.Jobs;
using Elsa.Retention.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Retention.Feature;

/// <summary>
/// The retention features provides automated cleanup of workflow instances
/// </summary>
public class RetentionFeature : FeatureBase
{

    /// <summary>
    /// Gets or sets a delegate to configure the retention options.
    /// </summary>
    public Action<CleanupOptions> ConfigureCleanupOptions { get; set; } = _ => { };
    
    /// <summary>
    /// Create the retention feature
    /// </summary>
    /// <param name="module"></param>
    public RetentionFeature(IModule module) : base(module)
    {
    }
    

    /// <inheritdoc cref="FeatureBase"/>
    public override void Apply()
    {
        Services.Configure(ConfigureCleanupOptions);
        Services.AddTransient<CleanupJob>();

    }

    /// <inheritdoc cref="FeatureBase"/>
    public override void ConfigureHostedServices()
    {
        ConfigureHostedService<CleanupHostedService>();
    }
}