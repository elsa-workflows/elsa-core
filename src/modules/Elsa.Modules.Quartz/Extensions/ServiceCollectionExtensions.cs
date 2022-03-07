using Elsa.Jobs.Contracts;
using Elsa.Modules.Quartz.Contracts;
using Elsa.Modules.Quartz.Jobs;
using Elsa.Modules.Quartz.Services;
using Elsa.Modules.Scheduling.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using IJob = Elsa.Jobs.Contracts.IJob;

namespace Elsa.Modules.Quartz.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers both Quartz as well as Elsa-specific services and jobs.
    /// If you register Quartz yourself, use <see cref="AddQuartzModule"/> instead.
    /// </summary>
    public static IServiceCollection AddQuartzAndModule(
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
                AddQuartzModule(services, configure);
            })
            .AddQuartzHostedService(options => ConfigureQuartzHostedService(options, configureQuartzHostedService));
    }

    /// <summary>
    /// This will register Elsa-specific services and jobs, but will **not** register Quartz itself. To register Quartz, you need to do so yourself, or use <see cref="AddQuartzAndModule"/> to register & configure Quartz for Elsa.
    /// </summary>
    public static IServiceCollection AddQuartzModule(this IServiceCollection services, IServiceCollectionQuartzConfigurator quartz)
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
        quartz.AddJob<ResumeWorkflowJob>();

        return quartz;
    }

    private static void ConfigureQuartzHostedService(QuartzHostedServiceOptions options, Action<QuartzHostedServiceOptions>? configureQuartzHostedService)
    {
        options.WaitForJobsToComplete = true;
        configureQuartzHostedService?.Invoke(options);
    }

    private static IServiceCollectionQuartzConfigurator AddJob<TJob>(this IServiceCollectionQuartzConfigurator quartz) where TJob : IJob =>
        quartz.AddJob<QuartzJob<TJob>>(job => job.StoreDurably().WithIdentity(typeof(TJob).Name));

    private static void ConfigureQuartz(IServiceCollectionQuartzConfigurator quartz, Action<IServiceCollectionQuartzConfigurator>? configureQuartz)
    {
        quartz.UseMicrosoftDependencyInjectionJobFactory();
        quartz.UseSimpleTypeLoader();
        quartz.UseInMemoryStore();
        configureQuartz?.Invoke(quartz);
    }
}