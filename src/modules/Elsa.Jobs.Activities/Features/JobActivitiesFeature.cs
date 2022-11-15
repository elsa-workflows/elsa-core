using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Jobs.Activities.Implementations;
using Elsa.Jobs.Activities.Services;
using Elsa.Jobs.Features;
using Elsa.Mediator.Extensions;
using Elsa.Mediator.Features;
using Elsa.Workflows.Management.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Jobs.Activities.Features;

[DependsOn(typeof(MediatorFeature))]
[DependsOn(typeof(JobsFeature))]
public class JobActivitiesFeature : FeatureBase
{
    public JobActivitiesFeature(IModule module) : base(module)
    {
    }
    
    public override void Apply()
    {
        Services
            .AddSingleton<IJobRegistry, JobRegistry>()
            .AddActivityProvider<JobActivityProvider>()
            ;

        Services.AddNotificationHandlersFrom(GetType());
    }
}