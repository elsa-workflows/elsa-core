using Elsa.Activities.Jobs.Implementations;
using Elsa.Activities.Jobs.Services;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Mediator.Extensions;
using Elsa.Mediator.Features;
using Elsa.Workflows.Management.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Jobs.Features;

[DependsOn(typeof(MediatorFeature))]
public class JobsFeature : FeatureBase
{
    public JobsFeature(IModule module) : base(module)
    {
    }
    
    public override void Apply()
    {
        Services
            .AddSingleton<IJobRegistry, JobRegistry>()
            .AddSingleton<IActivityProvider, JobActivityProvider>()
            ;

        Services.AddNotificationHandlersFrom(GetType());
    }
}