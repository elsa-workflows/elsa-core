using Elsa.ActivityDefinitions.Entities;
using Elsa.ActivityDefinitions.EntityFrameworkCore.Implementations;
using Elsa.ActivityDefinitions.Features;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Persistence.EntityFrameworkCore.Common.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ActivityDefinitions.EntityFrameworkCore.Features;

[DependsOn(typeof(ActivityDefinitionsFeature))]
public class EFCoreActivityDefinitionsPersistenceFeature : EFCorePersistenceFeature<ActivityDefinitionsDbContext>
{
    public EFCoreActivityDefinitionsPersistenceFeature(IModule module) : base(module)
    {
    }

    public override void Configure()
    {
        Module.UseCustomActivities(labels => labels
            .WithActivityDefinitionStore(sp => sp.GetRequiredService<EFCoreActivityDefinitionStore>())
        );
    }

    public override void Apply()
    {
        base.Apply();

        AddStore<ActivityDefinition, EFCoreActivityDefinitionStore>(Services);
    }
}