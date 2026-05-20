using Elsa.Abstractions;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Filters;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.BookmarkQueueDeadLetters.Get;

[UsedImplicitly]
internal class Endpoint(IBookmarkQueueDeadLetterStore store) : ElsaEndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/bookmark-queue/dead-letters/{id}");
        ConfigurePermissions(PermissionNames.ReadBookmarkQueueDeadLetters);
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var id = Route<string>("id")!;
        var item = await store.FindAsync(new BookmarkQueueDeadLetterFilter { Id = id }, cancellationToken);

        if (item == null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        await Send.OkAsync(item, cancellationToken);
    }
}
