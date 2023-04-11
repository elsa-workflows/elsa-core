using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Elsa.Quartz.Features;

/// <summary>
/// A feature that installs and configures Quartz.NET. Only enable this feature if you are not configuring Quartz.NET yourself.
/// </summary>
public class QuartzFeature : FeatureBase
{
    /// <inheritdoc />
    public QuartzFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// A delegate that can be used to configure Quartz.NET options.
    /// </summary>
    public Action<QuartzOptions>? ConfigureQuartzOptions { get; set; }
    
    /// <summary>
    /// A delegate that can be used to configure Quartz.NET itself.
    /// </summary>
    public Action<IServiceCollectionQuartzConfigurator>? ConfigureQuartz { get; set; }
    
    /// <summary>
    /// A delegate that can be used to configure Quartz.NET hosted service.
    /// </summary>
    public Action<QuartzHostedServiceOptions>? ConfigureQuartzHostedService { get; set; } = options => options.WaitForJobsToComplete = true;

    /// <inheritdoc />
    public override void ConfigureHostedServices()
    {
        Services.AddQuartzHostedService(ConfigureQuartzHostedService);
    }

    /// <inheritdoc />
    public override void Apply()
    {
        if (ConfigureQuartzOptions != null)
            Services.Configure(ConfigureQuartzOptions);

        Services
            .AddQuartz(configure =>
            {
                ConfigureQuartzInternal(configure, ConfigureQuartz);
            });
    }

    private static void ConfigureQuartzInternal(IServiceCollectionQuartzConfigurator quartz, Action<IServiceCollectionQuartzConfigurator>? configureQuartz)
    {
        quartz.UseMicrosoftDependencyInjectionJobFactory();
        quartz.UseSimpleTypeLoader();
        quartz.UseInMemoryStore();
        configureQuartz?.Invoke(quartz);
    }
}