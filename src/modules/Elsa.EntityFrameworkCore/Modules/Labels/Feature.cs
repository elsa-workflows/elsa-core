using Elsa.EntityFrameworkCore.Common;
using Elsa.EntityFrameworkCore.Common.Contracts;
using Elsa.EntityFrameworkCore.Handlers;
using Elsa.Extensions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Labels.Entities;
using Elsa.Labels.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.EntityFrameworkCore.Modules.Labels;

[DependsOn(typeof(LabelsFeature))]
public class EFCoreLabelPersistenceFeature(IModule module) : PersistenceFeatureBase<LabelsElsaDbContext>(module)
{
    /// Delegate for determining the exception handler.
    public Func<IServiceProvider, IDbExceptionHandler<LabelsElsaDbContext>> DbExceptionHandler { get; set; } = _ => new NoopDbExceptionHandler();

    public override void Configure()
    {
        Module.UseLabels(labels =>
        {
            labels.LabelStore = sp => sp.GetRequiredService<EFCoreLabelStore>();
            labels.WorkflowDefinitionLabelStore = sp => sp.GetRequiredService<EFCoreWorkflowDefinitionLabelStore>();
        });
    }

    public override void Apply()
    {
        base.Apply();

        Services.AddScoped(DbExceptionHandler);

        AddEntityStore<Label, EFCoreLabelStore>();
        AddEntityStore<WorkflowDefinitionLabel, EFCoreWorkflowDefinitionLabelStore>();
    }
}