using Elsa.CustomActivities.Entities;
using Elsa.CustomActivities.Implementations;
using Elsa.CustomActivities.Services;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Mediator.Extensions;
using Elsa.Persistence.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.CustomActivities.Features;

public class CustomActivitiesFeature : FeatureBase
{
    public CustomActivitiesFeature(IModule module) : base(module)
    {
    }

    public Func<IServiceProvider, IActivityDefinitionStore> ActivityDefinitionStore { get; set; } = sp => sp.GetRequiredService<MemoryActivityDefinitionStore>();

    public CustomActivitiesFeature WithActivityDefinitionStore(Func<IServiceProvider, IActivityDefinitionStore> factory)
    {
        ActivityDefinitionStore = factory;
        return this;
    }
    
    public override void Apply()
    {
        Services
            .AddMemoryStore<ActivityDefinition, MemoryActivityDefinitionStore>()
            .AddSingleton(ActivityDefinitionStore)
            .AddSingleton<IActivityDefinitionPublisher, ActivityDefinitionPublisher>()
            ;

        Services.AddNotificationHandlersFrom(GetType());
    }
}