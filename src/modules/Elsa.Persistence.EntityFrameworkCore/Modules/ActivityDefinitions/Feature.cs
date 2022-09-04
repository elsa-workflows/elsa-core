using Elsa.ActivityDefinitions.Entities;
using Elsa.ActivityDefinitions.Features;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Persistence.EntityFrameworkCore.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EntityFrameworkCore.Modules.ActivityDefinitions;

[DependsOn(typeof(ActivityDefinitionsFeature))]
public class EFCoreActivityDefinitionsPersistenceFeature : PersistenceFeatureBase<ActivityDefinitionsDbContext>
{
    public EFCoreActivityDefinitionsPersistenceFeature(IModule module) : base(module)
    {
    }

    public override void Configure()
    {
        Module.UseActivityDefinitions(labels => { labels.ActivityDefinitionStore = sp => sp.GetRequiredService<EFCoreActivityDefinitionStore>(); });
    }

    public override void Apply()
    {
        base.Apply();
        AddStore<ActivityDefinition, EFCoreActivityDefinitionStore>();
    }
}