using Elsa.ActivityDefinitions.Entities;
using Elsa.ActivityDefinitions.Implementations;
using Elsa.ActivityDefinitions.Services;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Mediator.Extensions;
using Elsa.Persistence.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ActivityDefinitions.Features;

public class ActivityDefinitionsFeature : FeatureBase
{
    public ActivityDefinitionsFeature(IModule module) : base(module)
    {
    }

    public Func<IServiceProvider, IActivityDefinitionStore> ActivityDefinitionStore { get; set; } = sp => sp.GetRequiredService<MemoryActivityDefinitionStore>();

    public ActivityDefinitionsFeature WithActivityDefinitionStore(Func<IServiceProvider, IActivityDefinitionStore> factory)
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