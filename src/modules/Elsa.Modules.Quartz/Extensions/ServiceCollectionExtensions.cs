using Elsa.Activities.Scheduling.Contracts;
using Elsa.Activities.Scheduling.Jobs;
using Elsa.Modules.Quartz.Contracts;
using Elsa.Modules.Quartz.Jobs;
using Elsa.Modules.Quartz.Services;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using IElsaJob = Elsa.Activities.Scheduling.Contracts.IJob;

namespace Elsa.Modules.Quartz.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// This will register both Quartz as well as Elsa-specific services and jobs.
    /// If you prefer to register Quartz yourself, use <see cref="ConfigureQuartzModule"/>
    /// </summary>
    public static IServiceCollection AddQuartzModule(
        this IServiceCollection services,
        Action<QuartzOptions>? configureQuartzOptions = default,
        Action<IServiceCollectionQuartzConfigurator>? configureQuartz = default,
        Action<QuartzHostedServiceOptions>? configureQuartzHostedService = default)
    {
        if (configureQuartzOptions != null)
            services.Configure(configureQuartzOptions);

        return services
            .AddQuartz(configure =>
            {
                ConfigureQuartz(configure, configureQuartz);
                ConfigureQuartzModule(services, configure);
            })
            .AddQuartzHostedService(options => ConfigureQuartzHostedService(options, configureQuartzHostedService));
    }

    /// <summary>
    /// This will register Elsa-specific services and jobs, but will **not** register Quartz itself. To register Quartz, you need to do so yourself, or use <see cref="AddQuartzModule"/> to register & configure Quartz for Elsa.
    /// </summary>
    public static IServiceCollection ConfigureQuartzModule(this IServiceCollection services, IServiceCollectionQuartzConfigurator quartz)
    {
        services
            .AddSingleton<IElsaJobSerializer, ElsaJobSerializer>()
            .AddSingleton<IJobScheduler, QuartzJobScheduler>();
        
        quartz.AddElsaJobs();

        return services;
    }

    private static IServiceCollectionQuartzConfigurator AddElsaJobs(this IServiceCollectionQuartzConfigurator quartz)
    {
        quartz.AddJob<RunWorkflowJob>();

        return quartz;
    }

    private static void ConfigureQuartzHostedService(QuartzHostedServiceOptions options, Action<QuartzHostedServiceOptions>? configureQuartzHostedService)
    {
        options.WaitForJobsToComplete = true;
        configureQuartzHostedService?.Invoke(options);
    }

    private static IServiceCollectionQuartzConfigurator AddJob<TJob>(this IServiceCollectionQuartzConfigurator quartz) where TJob : IElsaJob =>
        quartz.AddJob<QuartzJob<RunWorkflowJob>>(job => job.StoreDurably().WithIdentity(nameof(RunWorkflowJob)));

    private static void ConfigureQuartz(IServiceCollectionQuartzConfigurator quartz, Action<IServiceCollectionQuartzConfigurator>? configureQuartz)
    {
        quartz.UseMicrosoftDependencyInjectionJobFactory();
        quartz.AddJob<QuartzJob<RunWorkflowJob>>(job => job.StoreDurably().WithIdentity(nameof(RunWorkflowJob)));
        quartz.UseSimpleTypeLoader();
        quartz.UseInMemoryStore();
        configureQuartz?.Invoke(quartz);
    }
}