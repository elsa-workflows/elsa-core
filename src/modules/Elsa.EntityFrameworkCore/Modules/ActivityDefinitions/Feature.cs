using Elsa.ActivityDefinitions.Entities;
using Elsa.ActivityDefinitions.Features;
using Elsa.EntityFrameworkCore.Common;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.EntityFrameworkCore.Modules.ActivityDefinitions;

[DependsOn(typeof(ActivityDefinitionsFeature))]
public class EFCoreActivityDefinitionsPersistenceFeature : PersistenceFeatureBase<ActivityDefinitionsElsaDbContext>
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