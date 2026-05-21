using Elsa.Abstractions;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Filters;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.BookmarkQueueDeadLetters.Delete;

[UsedImplicitly]
internal class Endpoint(IBookmarkQueueDeadLetterStore store) : ElsaEndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/bookmark-queue/dead-letters/{id}");
        ConfigurePermissions(PermissionNames.DeleteBookmarkQueueDeadLetters);
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var id = Route<string>("id")!;
        var count = await store.DeleteAsync(new BookmarkQueueDeadLetterFilter { Id = id }, cancellationToken);

        if (count == 0)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        await Send.NoContentAsync(cancellationToken);
    }
}
