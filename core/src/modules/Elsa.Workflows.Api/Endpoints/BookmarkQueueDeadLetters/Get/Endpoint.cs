using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Workflows.Api.Endpoints.BookmarkQueueDeadLetters;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Filters;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.BookmarkQueueDeadLetters.Get;

[UsedImplicitly]
internal class Endpoint(IBookmarkQueueDeadLetterStore store) : ElsaEndpointWithoutRequest<BookmarkQueueDeadLetterModel>
{
    public override void Configure()
    {
        Get("/bookmark-queue/dead-letters/{id}");
        RequirePermission(Elsa.Workflows.Api.Permissions.WorkflowPermissions.BookmarkQueueDeadLetters, CoreVerbs.View);
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

        await Send.OkAsync(BookmarkQueueDeadLetterModel.FromEntity(item), cancellationToken);
    }
}
