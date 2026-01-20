using CShells.Features;
using Elsa.Labels.Entities;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.Modules.Labels;

/// <summary>
/// Configures the labels feature with an Entity Framework Core persistence provider.
/// </summary>
[ShellFeature(
    DisplayName = "EF Core Label Persistence",
    Description = "Provides Entity Framework Core persistence for label management",
    DependsOn = ["Labels"])]
[UsedImplicitly]
public class EFCoreLabelPersistenceShellFeature : PersistenceShellFeatureBase<EFCoreLabelPersistenceShellFeature, LabelsElsaDbContext>
{
    protected override void OnConfiguring(IServiceCollection services)
    {
        AddEntityStore<Label, EFCoreLabelStore>(services);
        AddEntityStore<WorkflowDefinitionLabel, EFCoreWorkflowDefinitionLabelStore>(services);
    }
}
