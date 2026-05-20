using Elsa.Abstractions;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Workflows.Api.Endpoints.BookmarkQueueDeadLetters;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.BookmarkQueueDeadLetters.List;

[UsedImplicitly]
internal class Endpoint(IBookmarkQueueDeadLetterStore store) : ElsaEndpoint<ListRequest, ListResponse>
{
    public override void Configure()
    {
        Verbs(FastEndpoints.Http.GET, FastEndpoints.Http.POST);
        Routes("/bookmark-queue/dead-letters");
        ConfigurePermissions(PermissionNames.ReadBookmarkQueueDeadLetters);
    }

    public override async Task HandleAsync(ListRequest request, CancellationToken cancellationToken)
    {
        var pageArgs = PageArgs.FromPage(request.Page ?? 0, request.PageSize ?? 50);
        var filter = new BookmarkQueueDeadLetterFilter
        {
            WorkflowInstanceId = request.WorkflowInstanceId
        };
        var order = new BookmarkQueueDeadLetterItemOrder<DateTimeOffset>(x => x.DeadLetteredAt, OrderDirection.Descending);
        var page = await store.PageAsync(pageArgs, filter, order, cancellationToken);
        var response = new ListResponse(page.Items, page.TotalCount);

        await Send.OkAsync(response, cancellationToken);
    }
}
