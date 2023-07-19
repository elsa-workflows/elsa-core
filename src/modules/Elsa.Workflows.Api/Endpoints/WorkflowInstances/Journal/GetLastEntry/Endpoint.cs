using Elsa.Abstractions;
using Elsa.Common.Entities;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.Journal.GetLastEntry;

/// <summary>
/// Return the last log entry for the specified workflow instance and activity ID.
/// </summary>
[PublicAPI]
public class Get : ElsaEndpoint<Request, WorkflowExecutionLogRecord>
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
        Get("/workflow-instances/{workflowInstanceId}/journal/{activityId}");
        ConfigurePermissions("read:workflow-instances");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var filter = new WorkflowExecutionLogRecordFilter
        {
            WorkflowInstanceId = request.WorkflowInstanceId,
            ActivityId = request.ActivityId,
            AnyEventName = new[] { "Started", "Completed", "Faulted" }
        };

        var sort = new WorkflowExecutionLogRecordOrder<DateTimeOffset>(
            x => x.Timestamp,
            OrderDirection.Descending
        );

        var entry = await _store.FindAsync(filter, sort, cancellationToken);

        if (entry == null)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        await SendOkAsync(entry, cancellationToken);
    }
}