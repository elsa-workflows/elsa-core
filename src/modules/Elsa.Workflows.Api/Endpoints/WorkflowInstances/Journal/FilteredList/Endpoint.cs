using Elsa.Abstractions;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.Journal.FilteredList;

/// <summary>
/// Gets the journal for a workflow instance.
/// </summary>
[PublicAPI]
internal class Get : ElsaEndpoint<Request, Response>
{
    private readonly IWorkflowExecutionLogStore _store;

    /// <inheritdoc />
    public Get(IWorkflowExecutionLogStore store)
    {
        _store = store;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/workflow-instances/{id}/journal");
        ConfigurePermissions("read:workflow-instances");
    }

    /// <inheritdoc />
    public override async Task<Response> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var pageArgs = PageArgs.From(request.Page, request.PageSize, request.Skip, request.Take);
        var filter = new WorkflowExecutionLogRecordFilter
        {
            WorkflowInstanceId = request.WorkflowInstanceId,
            ActivityIds = request.Filter?.ActivityIds
        };
        var order = new WorkflowExecutionLogRecordOrder<long>(x => x.Sequence, OrderDirection.Ascending);
        var pageOfRecords = await _store.FindManyAsync(filter, pageArgs, order, cancellationToken);

        var models = pageOfRecords.Items.Select(x =>
                new ExecutionLogRecord(
                    x.Id,
                    x.ActivityInstanceId,
                    x.ParentActivityInstanceId,
                    x.ActivityId,
                    x.ActivityType,
                    x.ActivityTypeVersion,
                    x.ActivityName,
                    x.NodeId,
                    x.Timestamp,
                    x.Sequence,
                    x.EventName,
                    x.Message,
                    x.Source,
                    x.ActivityState,
                    x.Payload))
            .ToList();

        return new(models, pageOfRecords.TotalCount);
    }
}