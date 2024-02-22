using Elsa.Dapper.Features;
using Elsa.Dapper.Modules.Runtime.Stores;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.KeyValues.Features;
using Elsa.Workflows.Runtime.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Dapper.Modules.Runtime.Features;

/// <summary>
/// Configures the default workflow runtime to use Dapper persistence providers.
/// </summary>
[DependsOn(typeof(DapperFeature))]
[PublicAPI]
public class DapperWorkflowRuntimePersistenceFeature : FeatureBase
{
    /// <inheritdoc />
    public DapperWorkflowRuntimePersistenceFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<KeyValueFeature>(feature =>
        {
            feature.KeyValueStore = sp => sp.GetRequiredService<DapperKeyValueStore>();
        });
        Module.Configure<WorkflowRuntimeFeature>(feature =>
        {
            feature.TriggerStore = sp => sp.GetRequiredService<DapperTriggerStore>();
            feature.BookmarkStore = sp => sp.GetRequiredService<DapperBookmarkStore>();
            feature.WorkflowInboxStore = sp => sp.GetRequiredService<DapperWorkflowInboxMessageStore>();
            feature.WorkflowExecutionLogStore = sp => sp.GetRequiredService<DapperWorkflowExecutionLogStore>();
            feature.ActivityExecutionLogStore = sp => sp.GetRequiredService<DapperActivityExecutionRecordStore>();
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();

        Services.AddScoped<DapperTriggerStore>();
        Services.AddScoped<DapperBookmarkStore>();
        Services.AddScoped<DapperWorkflowInboxMessageStore>();
        Services.AddScoped<DapperWorkflowExecutionLogStore>();
        Services.AddScoped<DapperActivityExecutionRecordStore>();
        Services.AddScoped<DapperKeyValueStore>();
    }
}