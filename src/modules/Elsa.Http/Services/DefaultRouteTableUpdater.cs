using Elsa.Extensions;
using Elsa.Http.Bookmarks;
using Elsa.Http.Contexts;
using Elsa.Workflows;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;

namespace Elsa.Http.Services;

/// <inheritdoc />
public class DefaultRouteTableUpdater(
    IRouteTable routeTable, 
    ITriggerStore triggerStore, 
    IBookmarkStore bookmarkStore, 
    IHttpEndpointRoutesProvider httpEndpointRoutesProvider)
    : IRouteTableUpdater
{
    /// <inheritdoc />
    public async Task UpdateAsync(CancellationToken cancellationToken = default)
    {
        var bookmarkName = ActivityTypeNameHelper.GenerateTypeName<HttpEndpoint>();
        var triggerFilter = new TriggerFilter
        {
            Name = bookmarkName
        };
        var bookmarkFilter = new BookmarkFilter
        {
            Name = bookmarkName
        };
        var triggers = (await triggerStore.FindManyAsync(triggerFilter, cancellationToken)).ToList();
        var bookmarks = (await bookmarkStore.FindManyAsync(bookmarkFilter, cancellationToken)).ToList();

        await AddRoutesAsync(triggers, cancellationToken);
        await AddRoutesAsync(bookmarks, cancellationToken);
    }

    public async Task AddRoutesAsync(IEnumerable<StoredTrigger> triggers, CancellationToken cancellationToken = default)
    {
        var httpEndpointTriggers = Filter(triggers).ToList();

        foreach (var trigger in httpEndpointTriggers)
        {
            var payload = trigger.GetPayload<HttpEndpointBookmarkPayload>();
            await AddRoutesAsync(payload, trigger.TenantId, cancellationToken);
        }
    }

    public async Task AddRoutesAsync(IEnumerable<StoredBookmark> bookmarks, CancellationToken cancellationToken = default)
    {
        var httpEndpointBookmarks = Filter(bookmarks).ToList();

        foreach (var bookmark in httpEndpointBookmarks)
        {
            var payload = bookmark.GetPayload<HttpEndpointBookmarkPayload>();
            await AddRoutesAsync(payload, bookmark.TenantId, cancellationToken);
        }
    }

    public async Task AddRoutesAsync(IEnumerable<Bookmark> bookmarks, WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken = default)
    {
        var httpEndpointBookmarks = Filter(bookmarks).ToList();

        foreach (var bookmark in httpEndpointBookmarks)
        {
            var payload = bookmark.GetPayload<HttpEndpointBookmarkPayload>();
            await AddRoutesAsync(payload, workflowExecutionContext.Workflow.Identity.TenantId, cancellationToken);
        }
    }

    public void RemoveRoutes(IEnumerable<StoredTrigger> triggers)
    {
        var paths = Filter(triggers).Select(x => x.GetPayload<HttpEndpointBookmarkPayload>().Path).ToList();
        routeTable.RemoveRange(paths);
    }

    public void RemoveRoutes(IEnumerable<Bookmark> bookmarks)
    {
        var paths = Filter(bookmarks).Select(x => x.GetPayload<HttpEndpointBookmarkPayload>().Path).ToList();
        routeTable.RemoveRange(paths);
    }

    private async Task AddRoutesAsync(HttpEndpointBookmarkPayload payload, string? tenantId, CancellationToken cancellationToken)
    {
        var context = new HttpEndpointRouteProviderContext(payload, tenantId, cancellationToken);
        var routes = await httpEndpointRoutesProvider.GetRoutesAsync(context);
            
        foreach (var route in routes)
            routeTable.Add(route);
    }

    private static IEnumerable<StoredTrigger> Filter(IEnumerable<StoredTrigger> triggers)
    {
        var triggerName = ActivityTypeNameHelper.GenerateTypeName<HttpEndpoint>();
        return triggers.Where(x => x.Name == triggerName && x.Payload != null);
    }

    private static IEnumerable<StoredBookmark> Filter(IEnumerable<StoredBookmark> bookmarks)
    {
        var activityTypeName = ActivityTypeNameHelper.GenerateTypeName<HttpEndpoint>();
        return bookmarks.Where(x => x.Name == activityTypeName && x.Payload != null);
    }

    private static IEnumerable<Bookmark> Filter(IEnumerable<Bookmark> bookmarks)
    {
        var bookmarkName = ActivityTypeNameHelper.GenerateTypeName<HttpEndpoint>();
        return bookmarks.Where(x => x.Name == bookmarkName && x.Payload != null);
    }
}