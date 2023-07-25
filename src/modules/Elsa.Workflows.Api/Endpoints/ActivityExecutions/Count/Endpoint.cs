using Elsa.Abstractions;
using Elsa.Models;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Filters;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.ActivityExecutions.Count;

/// <summary>
/// Counts the number of executions for a given activity.
/// </summary>
[PublicAPI]
internal class Count : ElsaEndpoint<Request, CountResponse>
{
    private readonly IActivityExecutionStore _store;

    /// <inheritdoc />
    public Count(IActivityExecutionStore store)
    {
        _store = store;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/activity-executions/count");
        ConfigurePermissions("read:activity-execution");
    }

    /// <inheritdoc />
    public override async Task<CountResponse> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var filter = new ActivityExecutionRecordFilter
        {
            WorkflowInstanceId = request.WorkflowInstanceId,
            ActivityId = request.ActivityId,
            Completed = request.Completed
        };
        var count = await _store.CountAsync(filter, cancellationToken);
        return new CountResponse(count);
    }
}