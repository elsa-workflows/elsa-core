using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Workflows.Runtime.Contracts;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.Journal.List;

/// <summary>
/// Gets the journal for a workflow instance.
/// </summary>
[PublicAPI]
public class Get : ElsaEndpoint<Request, Response>
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
        Get("/workflow-instances/{id}/journal");
        ConfigurePermissions("read:workflow-instances");
    }

    /// <inheritdoc />
    public override async Task<Response> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var pageArgs = new PageArgs(request.Page, request.PageSize);
        var filter = new WorkflowExecutionLogRecordFilter { WorkflowInstanceId = request.WorkflowInstanceId };
        var pageOfRecords = await _store.FindManyAsync(filter, pageArgs, cancellationToken);

        var models = pageOfRecords.Items.Select(x =>
                new ExecutionLogRecord(
                    x.Id,
                    x.ActivityInstanceId,
                    x.ParentActivityInstanceId,
                    x.ActivityId,
                    x.ActivityType,
                    x.Timestamp,
                    x.EventName,
                    x.Message,
                    x.Source,
                    x.ActivityState,
                    x.Payload))
            .ToList();

        return new(models, pageOfRecords.TotalCount);
    }
}