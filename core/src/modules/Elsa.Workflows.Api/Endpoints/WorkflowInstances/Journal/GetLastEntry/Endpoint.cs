using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Common.Entities;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.Journal.GetLastEntry;

/// <summary>
/// Return the last log entry for the specified workflow instance and activity ID.
/// </summary>
[PublicAPI]
public class Get(IWorkflowExecutionLogStore store) : ElsaEndpoint<Request, WorkflowExecutionLogRecord>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Get("/workflow-instances/{workflowInstanceId}/journal/{activityId}");
        RequirePermission(Elsa.Workflows.Api.Permissions.WorkflowPermissions.Instances, CoreVerbs.View);
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var filter = new WorkflowExecutionLogRecordFilter
        {
            WorkflowInstanceId = request.WorkflowInstanceId,
            ActivityId = request.ActivityId,
            EventNames = ["Started", "Completed", "Faulted"]
        };

        // Sequence is the instance-monotonic event cursor. Same-timestamp batches must not
        // pick an arbitrary "last" entry when only Timestamp descending is applied.
        var sort = new WorkflowExecutionLogRecordOrder<long>(
            x => x.Sequence,
            OrderDirection.Descending
        );

        var entry = await store.FindAsync(filter, sort, cancellationToken);

        if (entry == null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        await Send.OkAsync(entry, cancellationToken);
    }
}