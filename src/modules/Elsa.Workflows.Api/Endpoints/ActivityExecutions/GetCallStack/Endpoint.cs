using Elsa.Abstractions;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Filters;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.ActivityExecutions.GetCallStack;

/// <summary>
/// Gets the call stack (execution chain) for a given activity execution.
/// Returns activity execution records from root to the specified activity, ordered by call stack depth.
/// Supports pagination for deep call stacks.
/// </summary>
[PublicAPI]
internal class Endpoint(IActivityExecutionStore store) : ElsaEndpoint<Request, Response>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Get("/activity-executions/{id}/call-stack");
        ConfigurePermissions("read:activity-execution");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var id = Route<string>("id")!;
        var includeCrossWorkflowChain = request.IncludeCrossWorkflowChain ?? true;

        // Apply defaulting and upper bound to the requested page size to prevent excessive queries.
        const int defaultTake = 100;
        const int maxTake = 1000;

        var skip = request.Skip;
        var take = request.Take ?? defaultTake;

        if (take <= 0)
            take = defaultTake;

        if (take > maxTake)
            take = maxTake;
        
        // First, check if the activity execution exists.
        var idFilter = new ActivityExecutionRecordFilter
        {
            Id = id
        };
        var activityExecution = await store.FindAsync(idFilter, cancellationToken);
        if (activityExecution == null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        // Then, get the execution chain. An empty chain should result in 200 with an empty items array.
        var result = await store.GetExecutionChainAsync(id, includeCrossWorkflowChain, skip, take, cancellationToken);
        var response = new Response
        {
            ActivityExecutionId = id,
            Items = result.Items.ToList(),
            TotalCount = result.TotalCount
        };

        await Send.OkAsync(response, cancellationToken);
    }
}
