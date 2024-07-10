using Elsa.Abstractions;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.Journal.HasUpdates;

/// Endpoint that checks if there are updates for a workflow instance.
[PublicAPI]
internal class HasUpdates : ElsaEndpoint<Request, bool>
{
    private readonly IWorkflowExecutionLogStore _store;

    /// <inheritdoc />
    public HasUpdates(IWorkflowExecutionLogStore store)
    {
        _store = store;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/workflow-instances/{id}/journal/has-updates");
        ConfigurePermissions("read:workflow-instances");
    }
    
    /// <inheritdoc />
    public override async Task<bool> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var pageArgs = PageArgs.From(1, 1, 0, 1);
        var filter = new WorkflowExecutionLogRecordFilter { WorkflowInstanceId = request.WorkflowInstanceId };
        var order = new WorkflowExecutionLogRecordOrder<long>(x => x.Sequence, OrderDirection.Descending);
        var pageOfRecords = await _store.FindManyAsync(filter, pageArgs, order, cancellationToken);

        return pageOfRecords.Items.Any(item => item.Timestamp >= request.UpdatesSince);
    }
}