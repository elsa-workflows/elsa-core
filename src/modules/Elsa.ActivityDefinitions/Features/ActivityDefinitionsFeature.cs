using Elsa.ActivityDefinitions.Entities;
using Elsa.ActivityDefinitions.Implementations;
using Elsa.ActivityDefinitions.Services;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Mediator.Extensions;
using Elsa.Mediator.Features;
using Elsa.Persistence.Common.Extensions;
using Elsa.Workflows.Core.ActivityNodeResolvers;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Providers;
using Elsa.Workflows.Management.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ActivityDefinitions.Features;

[DependsOn(typeof(MediatorFeature))]
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
            .AddSingleton<IActivityDefinitionMaterializer, ActivityDefinitionMaterializer>()
            .AddSingleton<IActivityDefinitionPublisher, ActivityDefinitionPublisher>()
            .AddSingleton<IActivityProvider, ActivityDefinitionActivityProvider>()
            .AddSingleton<IActivityPortResolver, ActivityDefinitionPortResolver>()
            ;

        Services.AddNotificationHandlersFrom(GetType());
    }
}