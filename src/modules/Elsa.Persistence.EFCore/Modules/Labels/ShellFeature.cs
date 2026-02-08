using Elsa.Labels.Contracts;
using Elsa.Labels.Entities;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.Modules.Labels;

/// <summary>
/// Base class for label persistence features.
/// This is not a standalone shell feature - use provider-specific features.
/// </summary>
[UsedImplicitly]
public abstract class EFCoreLabelPersistenceShellFeatureBase : PersistenceShellFeatureBase<LabelsElsaDbContext>
{
    protected override void OnConfiguring(IServiceCollection services)
    {
        services.AddScoped<ILabelStore, EFCoreLabelStore>();
        services.AddScoped<IWorkflowDefinitionLabelStore, EFCoreWorkflowDefinitionLabelStore>();
        AddEntityStore<Label, EFCoreLabelStore>(services);
        AddEntityStore<WorkflowDefinitionLabel, EFCoreWorkflowDefinitionLabelStore>(services);
    }
}
