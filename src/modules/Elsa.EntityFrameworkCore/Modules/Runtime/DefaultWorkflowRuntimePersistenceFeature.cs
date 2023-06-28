using Elsa.EntityFrameworkCore.Common;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Runtime.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.EntityFrameworkCore.Modules.Runtime;

/// <summary>
/// Configures the default workflow runtime to use EF Core persistence providers.
/// </summary>
[DependsOn(typeof(WorkflowRuntimeFeature))]
[DependsOn(typeof(DefaultWorkflowRuntimeFeature))]
public class EFCoreDefaultWorkflowRuntimePersistenceFeature : PersistenceFeatureBase<RuntimeElsaDbContext>
{
    /// <inheritdoc />
    public EFCoreDefaultWorkflowRuntimePersistenceFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<DefaultWorkflowRuntimeFeature>(feature => { feature.WorkflowStateStore = sp => sp.GetRequiredService<EFCoreWorkflowStateStore>(); });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();

        AddEntityStore<WorkflowState, EFCoreWorkflowStateStore>();
    }
}