using Elsa.EntityFrameworkCore.Common;
using Elsa.Extensions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Labels.Entities;
using Elsa.Labels.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.EntityFrameworkCore.Modules.Labels;

/// <summary>
/// Configures the <see cref="LabelsFeature"/> feature with an Entity Framework Core persistence provider.
/// </summary>
[DependsOn(typeof(LabelsFeature))]
public class EFCoreLabelPersistenceFeature(IModule module) : PersistenceFeatureBase<EFCoreLabelPersistenceFeature, LabelsElsaDbContext>(module)
{
    /// <inheritdoc />
    public override void Configure()
    {
        Module.UseLabels(labels =>
        {
            labels.LabelStore = sp => sp.GetRequiredService<EFCoreLabelStore>();
            labels.WorkflowDefinitionLabelStore = sp => sp.GetRequiredService<EFCoreWorkflowDefinitionLabelStore>();
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();

        AddEntityStore<Label, EFCoreLabelStore>();
        AddEntityStore<WorkflowDefinitionLabel, EFCoreWorkflowDefinitionLabelStore>();
    }
}