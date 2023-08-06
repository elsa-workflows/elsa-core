using Elsa.Dapper.Modules.Runtime.Stores;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Runtime.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Dapper.Modules.Runtime.Features;

/// <summary>
/// Configures the default workflow runtime to use Dapper persistence providers.
/// </summary>
[DependsOn(typeof(WorkflowRuntimeFeature))]
[DependsOn(typeof(DefaultWorkflowRuntimeFeature))]
[DependsOn(typeof(DapperWorkflowRuntimePersistenceFeature))]
public class DapperDefaultWorkflowRuntimePersistenceFeature : FeatureBase
{
    /// <inheritdoc />
    public DapperDefaultWorkflowRuntimePersistenceFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<DefaultWorkflowRuntimeFeature>(feature => { feature.WorkflowStateStore = sp => sp.GetRequiredService<DapperWorkflowStateStore>(); });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddSingleton<DapperWorkflowStateStore>();
    }
}