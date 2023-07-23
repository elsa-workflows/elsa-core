using Elsa.Dapper.Features;
using Elsa.Dapper.Modules.Runtime.Services;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Dapper.Modules.Runtime.Features;

/// <summary>
/// A feature that registers the <see cref="DapperWorkflowExecutionLogStore"/> as the default <see cref="IWorkflowExecutionLogStore"/>.
/// </summary>
[DependsOn(typeof(WorkflowRuntimeFeature))]
[DependsOn(typeof(DapperFeature))]
public class EFCoreExecutionLogPersistenceFeature : FeatureBase
{
    /// <inheritdoc />
    public EFCoreExecutionLogPersistenceFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<WorkflowRuntimeFeature>(feature =>
        {
            feature.WorkflowExecutionLogStore = sp => sp.GetRequiredService<DapperWorkflowExecutionLogStore>();
            feature.ActivityExecutionLogStore = sp => sp.GetRequiredService<DapperActivityExecutionRecordStore>();
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services
            .AddSingleton<DapperWorkflowExecutionLogStore>()
            .AddSingleton<DapperActivityExecutionRecordStore>()
            ;
    }
}