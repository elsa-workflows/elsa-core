// using Elsa.Quartz.Jobs;
// using Elsa.Scheduling.Jobs;
// using Quartz;
// using IJob = Elsa.Jobs.Contracts.IJob;
//
// // ReSharper disable once CheckNamespace
// namespace Elsa.Extensions;
//
// /// <summary>
// /// Extends <see cref="IServiceCollectionQuartzConfigurator"/>.
// /// </summary>
// public static class DependencyInjectionExtensions
// {
//     public static IServiceCollectionQuartzConfigurator AddElsaJobs(this IServiceCollectionQuartzConfigurator quartz)
//     {
//         quartz.AddJob<RunWorkflowTask>();
//         quartz.AddJob<ResumeWorkflowJob>();
//
//         return quartz;
//     }
//
//     private static IServiceCollectionQuartzConfigurator AddJob<TJob>(this IServiceCollectionQuartzConfigurator quartz) where TJob : IJob =>
//         quartz.AddJob<QuartzJob<TJob>>(job => job.StoreDurably().WithIdentity(typeof(TJob).Name));
// }