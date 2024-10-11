using Elsa.Abstractions;
using Elsa.Common.Entities;
using Elsa.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.ActivityExecutionSummaries.ListSummaries;

/// <summary>
/// Lists a summary view of the executions for a given activity.
/// </summary>
[PublicAPI]
internal class Endpoint(IActivityExecutionStore store) : ElsaEndpoint<Request, ListResponse<ActivityExecutionRecordSummary>>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Get("/activity-execution-summaries/list");
        ConfigurePermissions("read:activity-execution");
    }

    /// <inheritdoc />
    public override async Task<ListResponse<ActivityExecutionRecordSummary>> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var filter = new ActivityExecutionRecordFilter
        {
            WorkflowInstanceId = request.WorkflowInstanceId,
            ActivityNodeId = request.ActivityNodeId,
            Completed = request.Completed
        };
        var order = new ActivityExecutionRecordOrder<DateTimeOffset>(x => x.StartedAt, OrderDirection.Ascending);
        var records = (await store.FindManySummariesAsync(filter, order, cancellationToken)).ToList();
        return new ListResponse<ActivityExecutionRecordSummary>(records);
    }
}