using Elsa.Abstractions;
using Elsa.Common.Entities;
using Elsa.Models;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.ActivityExecutions.List;

/// <summary>
/// Lists the executions for a given activity.
/// </summary>
[PublicAPI]
internal class List : ElsaEndpoint<Request, ListResponse<ActivityExecutionRecord>>
{
    private readonly IActivityExecutionStore _store;

    /// <inheritdoc />
    public List(IActivityExecutionStore store)
    {
        _store = store;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/activity-executions/list");
        ConfigurePermissions("read:activity-execution");
    }

    /// <inheritdoc />
    public override async Task<ListResponse<ActivityExecutionRecord>> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var filter = new ActivityExecutionRecordFilter
        {
            WorkflowInstanceId = request.WorkflowInstanceId,
            ActivityId = request.ActivityId,
            Completed = request.Completed
        };
        var order = new ActivityExecutionRecordOrder<DateTimeOffset>(x => x.StartedAt, OrderDirection.Ascending);
        var records = (await _store.FindManyAsync(filter, order, cancellationToken)).ToList();
        return new ListResponse<ActivityExecutionRecord>(records);
    }
}