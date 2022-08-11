using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Extensions;
using Elsa.Features.Services;
using Elsa.Hangfire.Implementations;
using Elsa.Jobs.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Hangfire.Features;

[DependsOn(typeof(JobsFeature))]
[DependsOn(typeof(HangfireFeature))]
public class HangfireJobsFeature : FeatureBase
{
    public HangfireJobsFeature(IModule module) : base(module)
    {
    }

    public override void Configure()
    {
        Module.Use<JobsFeature>(f => f.JobQueueFactory = ActivatorUtilities.GetServiceOrCreateInstance<HangfireJobQueue>);
    }
}