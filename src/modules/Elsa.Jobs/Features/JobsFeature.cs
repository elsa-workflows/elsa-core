using System;
using Elsa.Common.Features;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Jobs.Extensions;
using Elsa.Jobs.HostedServices;
using Elsa.Jobs.Implementations;
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

    public override void Configure()
    {
        Services
            .AddSingleton<IJobSerializer, JobSerializer>()
            .AddSingleton<IJobFactory, JobFactory>()
            .AddSingleton<IJobRunner, JobRunner>();
    }

    public override void ConfigureHostedServices()
    {
        Services.AddHostedService<JobQueueHostedService>(sp =>
        {
            var workerCount = 10; // TODO: make configurable.
            return ActivatorUtilities.CreateInstance<JobQueueHostedService>(sp, workerCount);
        });
    }

    public override void Apply()
    {
        Services
            .AddSingleton(JobQueueFactory)
            .AddSingleton(JobSchedulerFactory)
            .CreateChannel<IJob>();
    }
}