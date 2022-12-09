using Elsa.ActivityDefinitions.Entities;
using Elsa.ActivityDefinitions.Implementations;
using Elsa.ActivityDefinitions.Services;
using Elsa.Common.Extensions;
using Elsa.Common.Features;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Mediator.Extensions;
using Elsa.Mediator.Features;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ActivityDefinitions.Features;

/// <summary>
/// Installs the activity definitions feature into the system.
/// </summary>
[DependsOn(typeof(MediatorFeature))]
[DependsOn(typeof(SystemClockFeature))]
public class ActivityDefinitionsFeature : FeatureBase
{
    /// <inheritdoc />
    public ActivityDefinitionsFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// A factory function that creates an instance of <see cref="IActivityDefinitionStore"/>.
    /// </summary>
    public Func<IServiceProvider, IActivityDefinitionStore> ActivityDefinitionStore { get; set; } = sp => sp.GetRequiredService<MemoryActivityDefinitionStore>();

    /// <inheritdoc />
    public override void Configure()
    {
        Module.AddFastEndpointsAssembly(GetType());
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services
            .AddMemoryStore<ActivityDefinition, MemoryActivityDefinitionStore>()
            .AddSingleton(ActivityDefinitionStore)
            .AddSingleton<IActivityDefinitionMaterializer, ActivityDefinitionMaterializer>()
            .AddSingleton<IActivityDefinitionPublisher, ActivityDefinitionPublisher>()
            .AddSingleton<IActivityPortResolver, ActivityDefinitionPortResolver>()
            .AddActivityProvider<ActivityDefinitionActivityProvider>()
            ;

        Services.AddNotificationHandlersFrom(GetType());
    }
}