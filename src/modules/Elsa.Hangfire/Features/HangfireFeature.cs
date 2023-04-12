using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Hangfire;
using Hangfire.MemoryStorage;
using Newtonsoft.Json;

namespace Elsa.Hangfire.Features;

/// <summary>
/// Sets up Hangfire. If you're setting up Hangfire yourself, then you should not enable this feature.
/// </summary>
public class HangfireFeature : FeatureBase
{
    /// <inheritdoc />
    public HangfireFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// A delegate that configures Hangfire.
    /// </summary>
    public Action<IServiceProvider, IGlobalConfiguration> ConfigureHangfire { get; set; } = (_, cfg) => cfg.UseMemoryStorage();

    /// <summary>
    /// A delegate that configures Hangfire's background job server options.
    /// </summary>
    public Action<IServiceProvider, BackgroundJobServerOptions> ConfigureBackgroundServerOptions { get; set; } = (_, _) => { };
    
    /// <summary>
    /// A delegate that creates a job storage instance.
    /// </summary>
    public Func<JobStorage> CreateJobStorage { get; set; } = () => new MemoryStorage();

    /// <inheritdoc />
    public override void Apply()
    {
        Action<IServiceProvider, IGlobalConfiguration> configAction = (sp, cfg) =>
        {
            cfg.UseSimpleAssemblyNameTypeSerializer();
            cfg.UseRecommendedSerializerSettings(json => json.TypeNameHandling = TypeNameHandling.Objects);
        };
        
        Action<IServiceProvider, BackgroundJobServerOptions> serverOptionsAction = (sp, options) =>
        {
            options.WorkerCount = 1;
            options.SchedulePollingInterval = TimeSpan.FromSeconds(1);
        };
        
        configAction += ConfigureHangfire;
        serverOptionsAction += ConfigureBackgroundServerOptions;
        
        Services.AddHangfire(configAction);
        Services.AddHangfireServer(serverOptionsAction, CreateJobStorage());
    }
}