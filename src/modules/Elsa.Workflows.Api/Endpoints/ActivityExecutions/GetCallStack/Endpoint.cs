using Elsa.Abstractions;
using Elsa.Workflows.Runtime;
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
        var id = Route<string>("id");
        var includeCrossWorkflowChain = request.IncludeCrossWorkflowChain ?? true;
        var skip = request.Skip;
        var take = request.Take;

        var result = await store.GetExecutionChainAsync(id, includeCrossWorkflowChain, skip, take, cancellationToken);

        if (result.TotalCount == 0)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        var response = new Response
        {
            ActivityExecutionId = id,
            Items = result.Items.ToList(),
            TotalCount = result.TotalCount,
            Skip = result.Skip,
            Take = result.Take
        };

        await Send.OkAsync(response, cancellationToken);
    }
}
