using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.MongoDb.Common;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.MongoDb.Modules.Runtime;

/// <summary>
/// A feature that registers the <see cref="MongoWorkflowExecutionLogStore"/> as the default <see cref="IWorkflowExecutionLogStore"/>.
/// </summary>
[DependsOn(typeof(WorkflowRuntimeFeature))]
public class MongoExecutionLogRecordPersistenceFeature : PersistenceFeatureBase
{
    /// <inheritdoc />
    public MongoExecutionLogRecordPersistenceFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<WorkflowRuntimeFeature>(feature =>
        {
            feature.WorkflowExecutionLogStore = sp => sp.GetRequiredService<MongoWorkflowExecutionLogStore>();
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();
        
        AddCollection<WorkflowExecutionLogRecord>("workflow_execution_logs");
        
        AddStore<WorkflowExecutionLogRecord, MongoWorkflowExecutionLogStore>();
        
        Services.AddHostedService<CreateIndices>();
    }
}