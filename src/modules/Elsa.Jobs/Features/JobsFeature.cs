using Elsa.Common.Features;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Jobs.Contracts;
using Elsa.Jobs.HostedServices;
using Elsa.Jobs.Options;
using Elsa.Jobs.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Jobs.Features;

[DependsOn(typeof(SystemClockFeature))]
public class JobsFeature : FeatureBase
{
    public JobsFeature(IModule module) : base(module)
    {
    }

    public Func<IServiceProvider, IJobQueue> JobQueueFactory { get; set; } = ActivatorUtilities.GetServiceOrCreateInstance<LocalJobQueue>;
    public Func<IServiceProvider, IJobScheduler> JobSchedulerFactory { get; set; } = ActivatorUtilities.GetServiceOrCreateInstance<LocalJobScheduler>;
    public Action<JobsOptions>? ConfigureOptions { get; set; }

    public override void Configure()
    {
        Services
            .AddSingleton<IJobSerializer, JobSerializer>()
            .AddSingleton<IJobFactory, JobFactory>()
            .AddSingleton<IJobRunner, JobRunner>();
    }

    public override void ConfigureHostedServices()
    {
        Services.AddHostedService<JobQueueHostedService>();
    }

    public override void Apply()
    {
        Services.Configure(ConfigureOptions ?? (_ => { }));

        Services
            .AddSingleton(JobQueueFactory)
            .AddSingleton(JobSchedulerFactory)
            .CreateChannel<IJob>();
    }
}