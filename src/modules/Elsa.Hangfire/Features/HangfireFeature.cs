using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Runtime.Features;
using Hangfire;
using Hangfire.MemoryStorage;
using Newtonsoft.Json;

namespace Elsa.Hangfire.Features;

/// <summary>
/// Sets up Hangfire. If you're setting up Hangfire yourself, then you should not enable this feature.
/// </summary>
[DependsOn(typeof(WorkflowRuntimeFeature))] // Ensure that the workflow runtime feature's hosted services have executed before Hangfire Server starts.
public class HangfireFeature(IModule module) : FeatureBase(module)
{
    /// <summary>
    /// A delegate that configures Hangfire.
    /// </summary>
    private Action<IServiceProvider, IGlobalConfiguration> _configureHangfire = (_, _) => { };

    /// <summary>
    /// A delegate that configures Hangfire's background job server options.
    /// </summary>
    private Action<IServiceProvider, BackgroundJobServerOptions> _configureBackgroundServerOptions = (_, _) => { };
    
    /// <summary>
    /// A delegate that creates a job storage instance.
    /// </summary>
    private Func<JobStorage> _createJobStorage  = () => new MemoryStorage();
    
    /// <summary>
    /// Configures Hangfire.
    /// </summary>
    public HangfireFeature ConfigureHangfire(Action<IServiceProvider, IGlobalConfiguration> configure)
    {
        _configureHangfire += configure;
        return this;
    } 
    
    /// <summary>
    /// Configures Hangfire's background job server options.
    /// </summary>
    public HangfireFeature ConfigureBackgroundServerOptions(Action<IServiceProvider, BackgroundJobServerOptions> configure)
    {
        _configureBackgroundServerOptions += configure;
        return this;
    }
    
    public HangfireFeature UseMemoryStorage()
    {
        return UseJobStorage(new MemoryStorage());
    }
    
    public HangfireFeature UseJobStorage(JobStorage storage)
    {
        _createJobStorage = () => storage;
        return this;
    }

    /// <inheritdoc />
    public override void Apply()
    {
        var jobStorage = _createJobStorage();
        
        Action<IServiceProvider, IGlobalConfiguration> configAction = (sp, cfg) =>
        {
            cfg.UseSimpleAssemblyNameTypeSerializer();
            cfg.UseRecommendedSerializerSettings(json => json.TypeNameHandling = TypeNameHandling.Objects);
            cfg.UseStorage(jobStorage);
        };
        
        Action<IServiceProvider, BackgroundJobServerOptions> serverOptionsAction = (sp, options) =>
        {
            options.WorkerCount = 1;
            options.SchedulePollingInterval = TimeSpan.FromSeconds(1);
        };
        
        configAction += _configureHangfire;
        serverOptionsAction += _configureBackgroundServerOptions;
        
        Services.AddHangfire(configAction);
        Services.AddHangfireServer(serverOptionsAction, jobStorage);
    }
}