using Elsa.Abstractions;
using Elsa.Workflows.Api.Endpoints.BookmarkQueueDeadLetters;
using Elsa.Workflows.Runtime;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.BookmarkQueueDeadLetters.Replay;

[UsedImplicitly]
internal class Endpoint(IBookmarkQueueDeadLetterManager manager) : ElsaEndpointWithoutRequest<ReplayResponse>
{
    public override void Configure()
    {
        Post("/bookmark-queue/dead-letters/{id}/replay");
        ConfigurePermissions(PermissionNames.ReplayBookmarkQueueDeadLetters);
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var id = Route<string>("id")!;
        var result = await manager.ReplayAsync(id, cancellationToken);

        if (result.Reason == "NotFound")
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        if (!result.Succeeded)
        {
            await Send.ResponseAsync(new ReplayResponse(false, result.QueueItemId, result.Reason), StatusCodes.Status409Conflict, cancellationToken);
            return;
        }

        await Send.OkAsync(new ReplayResponse(true, result.QueueItemId, null), cancellationToken);
    }
}
