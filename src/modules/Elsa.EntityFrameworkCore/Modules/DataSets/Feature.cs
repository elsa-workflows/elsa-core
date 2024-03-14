using Elsa.DataSets.Entities;
using Elsa.DataSets.Features;
using Elsa.EntityFrameworkCore.Common;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Identity.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.EntityFrameworkCore.Modules.DataSets;

/// <summary>
/// Configures the <see cref="IdentityFeature"/> feature with Entity Framework Core persistence providers.
/// </summary>
[DependsOn(typeof(IdentityFeature))]
[PublicAPI]
public class EFCoreDataSetPersistenceFeature : PersistenceFeatureBase<DataSetElsaDbContext>
{
    /// <inheritdoc />
    public EFCoreDataSetPersistenceFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<DataSetFeature>(feature =>
        {
            feature.WithDataSetStore(sp => sp.GetRequiredService<EFCoreDataSetDefinitionStore>());
            feature.WithLinkedServiceDefinitionStore(sp => sp.GetRequiredService<EFCoreLinkedServiceDefinitionStore>());
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();

        AddEntityStore<DataSetDefinition, EFCoreDataSetDefinitionStore>();
        AddEntityStore<LinkedServiceDefinition, EFCoreLinkedServiceDefinitionStore>();
    }
}