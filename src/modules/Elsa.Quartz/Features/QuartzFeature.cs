using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Elsa.Quartz.Features;

public class QuartzFeature : FeatureBase
{
    public QuartzFeature(IModule module) : base(module)
    {
    }

    public Action<QuartzOptions>? ConfigureQuartzOptions { get; set; }
    public Action<IServiceCollectionQuartzConfigurator>? ConfigureQuartz { get; set; }
    public Action<QuartzHostedServiceOptions>? ConfigureQuartzHostedService { get; set; }

    public override void Configure()
    {
        if (ConfigureQuartzOptions != null)
            Services.Configure(ConfigureQuartzOptions);

        Services
            .AddQuartz(configure =>
            {
                ConfigureQuartzInternal(configure, ConfigureQuartz);
                //configure.AddElsaJobs();
            });
    }

    public override void ConfigureHostedServices()
    {
        Services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
            ConfigureQuartzHostedService?.Invoke(options);
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