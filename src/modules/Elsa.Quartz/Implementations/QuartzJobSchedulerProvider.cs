using Elsa.Jobs.Services;
using Elsa.Quartz.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Elsa.Quartz.Implementations;

/// <summary>
/// A Quartz.NET implementation for <see cref="IJobSchedulerProvider"/>.
/// </summary>
public class QuartzJobSchedulerProvider : IJobSchedulerProvider
{
    public QuartzJobSchedulerProvider(bool registerQuartz = true)
    {
        RegisterQuartz = registerQuartz;
    }
    
    public QuartzJobSchedulerProvider(Action<QuartzOptions> configureQuartzOptions)
    {
        RegisterQuartz = true;
        ConfigureQuartzOptions = configureQuartzOptions;
    }
    
    public QuartzJobSchedulerProvider(
        Action<QuartzOptions> configureQuartzOptions,
        Action<IServiceCollectionQuartzConfigurator> configureQuartz)
    {
        RegisterQuartz = true;
        ConfigureQuartzOptions = configureQuartzOptions;
        ConfigureQuartz = configureQuartz;
    }

    public QuartzJobSchedulerProvider(
        Action<QuartzOptions> configureQuartzOptions,
        Action<IServiceCollectionQuartzConfigurator> configureQuartz,
        Action<QuartzHostedServiceOptions> configureQuartzHostedService)
    {
        RegisterQuartz = true;
        ConfigureQuartzOptions = configureQuartzOptions;
        ConfigureQuartz = configureQuartz;
        ConfigureQuartzHostedService = configureQuartzHostedService;
    }

    public bool RegisterQuartz { get; set; }
    public Action<QuartzOptions>? ConfigureQuartzOptions { get; set; }
    public Action<IServiceCollectionQuartzConfigurator>? ConfigureQuartz { get; set; }
    public Action<QuartzHostedServiceOptions>? ConfigureQuartzHostedService { get; set; }

    public void ConfigureServices(IServiceCollection services)
    {
        if (RegisterQuartz)
        {
            if (ConfigureQuartzOptions != null)
                services.Configure(ConfigureQuartzOptions);

            services
                .AddQuartz(configure =>
                {
                    ConfigureQuartzInternal(configure, ConfigureQuartz);
                    configure.AddElsaJobs();
                })
                .AddQuartzHostedService(options =>
                {
                    options.WaitForJobsToComplete = true;
                    ConfigureQuartzHostedService?.Invoke(options);
                });
        }

        services.AddSingleton<IJobScheduler, QuartzJobScheduler>();
        
    }

    private static void ConfigureQuartzInternal(IServiceCollectionQuartzConfigurator quartz, Action<IServiceCollectionQuartzConfigurator>? configureQuartz)
    {
        quartz.UseMicrosoftDependencyInjectionJobFactory();
        quartz.UseSimpleTypeLoader();
        quartz.UseInMemoryStore();
        configureQuartz?.Invoke(quartz);
    }
}