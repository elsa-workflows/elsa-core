using Elsa.ActivityDefinitions.Entities;
using Elsa.ActivityDefinitions.Implementations;
using Elsa.ActivityDefinitions.Services;
using Elsa.Common.Extensions;
using Elsa.Common.Features;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Mediator.Extensions;
using Elsa.Mediator.Features;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ActivityDefinitions.Features;

[DependsOn(typeof(MediatorFeature))]
[DependsOn(typeof(SystemClockFeature))]
public class ActivityDefinitionsFeature : FeatureBase
{
    public ActivityDefinitionsFeature(IModule module) : base(module)
    {
    }

    public Func<IServiceProvider, IActivityDefinitionStore> ActivityDefinitionStore { get; set; } = sp => sp.GetRequiredService<MemoryActivityDefinitionStore>();
    
    public override void Apply()
    {
        Services
            .AddMemoryStore<ActivityDefinition, MemoryActivityDefinitionStore>()
            .AddSingleton(ActivityDefinitionStore)
            .AddSingleton<IActivityDefinitionMaterializer, ActivityDefinitionMaterializer>()
            .AddSingleton<IActivityDefinitionPublisher, ActivityDefinitionPublisher>()
            .AddSingleton<IActivityProvider, ActivityDefinitionActivityProvider>()
            .AddSingleton<IActivityPortResolver, ActivityDefinitionPortResolver>()
            ;

        Services.AddNotificationHandlersFrom(GetType());
    }
}