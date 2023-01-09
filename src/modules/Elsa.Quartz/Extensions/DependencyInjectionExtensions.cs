using Elsa.Elasticsearch.Scheduling;
using Elsa.Quartz.Jobs;
using Elsa.Scheduling.Jobs;
using Quartz;
using IJob = Elsa.Jobs.Services.IJob;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollectionQuartzConfigurator AddElsaJobs(this IServiceCollectionQuartzConfigurator quartz)
    {
        quartz.AddJob<RunWorkflowJob>();
        quartz.AddJob<ResumeWorkflowJob>();
        quartz.AddJob<ConfigureElasticIndicesJob>();

        return quartz;
    }

    private static IServiceCollectionQuartzConfigurator AddJob<TJob>(this IServiceCollectionQuartzConfigurator quartz) where TJob : IJob =>
        quartz.AddJob<QuartzJob<TJob>>(job => job.StoreDurably().WithIdentity(typeof(TJob).Name));
}