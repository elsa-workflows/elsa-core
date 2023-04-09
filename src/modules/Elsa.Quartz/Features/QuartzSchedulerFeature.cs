// using Elsa.Features.Abstractions;
// using Elsa.Features.Attributes;
// using Elsa.Features.Services;
// using Elsa.Jobs.Features;
// using Elsa.Quartz.Services;
// using Microsoft.Extensions.DependencyInjection;
//
// namespace Elsa.Quartz.Features;
//
// [DependsOn(typeof(JobsFeature))]
// [DependsOn(typeof(QuartzFeature))]
// public class QuartzSchedulerFeature : FeatureBase
// {
//     public QuartzSchedulerFeature(IModule module) : base(module)
//     {
//     }
//
//     public override void Configure()
//     {
//         Module.Configure<JobsFeature>(f => f.JobSchedulerFactory = ActivatorUtilities.GetServiceOrCreateInstance<QuartzJobScheduler>);
//     }
// }