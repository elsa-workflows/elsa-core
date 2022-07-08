using Elsa.CustomActivities.Entities;
using Elsa.CustomActivities.EntityFrameworkCore.Implementations;
using Elsa.CustomActivities.Features;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Persistence.EntityFrameworkCore.Common.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.CustomActivities.EntityFrameworkCore.Features;

[DependsOn(typeof(CustomActivitiesFeature))]
public class EFCoreCustomActivitiesPersistenceFeature : EFCorePersistenceFeature<CustomActivitiesDbContext>
{
    public EFCoreCustomActivitiesPersistenceFeature(IModule module) : base(module)
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