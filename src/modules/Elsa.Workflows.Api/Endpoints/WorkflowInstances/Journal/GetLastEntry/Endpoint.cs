using Elsa.Abstractions;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.Journal.GetLastEntry;

/// <summary>
/// Gets the journal for a workflow instance.
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
        Get("/workflow-instances/{workflowInstanceId}/journal/{activityId}/{eventName}");
        ConfigurePermissions("read:workflow-instances");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var filter = new WorkflowExecutionLogRecordFilter
        {
            WorkflowInstanceId = request.WorkflowInstanceId,
            ActivityId = request.ActivityId,
            EventName = request.EventName
        };
        
        var entry = await _store.FindAsync(filter, cancellationToken);

        if (entry == null)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        await SendOkAsync(entry, cancellationToken);
    }
}