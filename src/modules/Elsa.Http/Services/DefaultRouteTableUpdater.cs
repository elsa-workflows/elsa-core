using Elsa.Extensions;
using Elsa.Http.Bookmarks;
using Elsa.Http.Contracts;
using Elsa.Http.Options;
using Elsa.Workflows;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Microsoft.Extensions.Options;

namespace Elsa.Http.Services;

/// <inheritdoc />
public class DefaultRouteTableUpdater(IRouteTable routeTable, ITriggerStore triggerStore, IBookmarkStore bookmarkStore, IOptions<HttpActivityOptions> options)
    : IRouteTableUpdater
{
    /// <inheritdoc />
    public async Task UpdateAsync(CancellationToken cancellationToken = default)
    {
        var bookmarkName = ActivityTypeNameHelper.GenerateTypeName<HttpEndpoint>();
        var triggerFilter = new TriggerFilter
        {
            Name = bookmarkName,
            TenantAgnostic = true
        };
        var bookmarkFilter = new BookmarkFilter
        {
            ActivityTypeName = bookmarkName,
            TenantAgnostic = true
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
            var path = trigger.GetPayload<HttpEndpointBookmarkStimulus>().Path;

            if (string.IsNullOrWhiteSpace(path))
                continue;

            var route = trigger.TenantId != null
                ? new[]
                {
                    "{tenantId}", options.Value.BasePath.ToString(), path
                }.JoinSegments()
                : path;

            routeTable.Add(route);
        }
    }

    public async Task AddRoutesAsync(IEnumerable<StoredBookmark> bookmarks, CancellationToken cancellationToken = default)
    {
        var httpEndpointBookmarks = Filter(bookmarks).ToList();

        foreach (var bookmark in httpEndpointBookmarks)
        {
            var path = bookmark.GetPayload<HttpEndpointBookmarkStimulus>().Path;

            if (string.IsNullOrWhiteSpace(path))
                continue;

            var tenantPath = new[]
            {
                "{tenantId}", options.Value.BasePath.ToString(), path
            }.JoinSegments();
            routeTable.Add(tenantPath);
        }
    }

    public async Task AddRoutesAsync(IEnumerable<Bookmark> bookmarks, WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken = default)
    {
        var httpEndpointBookmarks = Filter(bookmarks).ToList();

        foreach (var bookmark in httpEndpointBookmarks)
        {
            var path = bookmark.GetPayload<HttpEndpointBookmarkStimulus>().Path;

            if (string.IsNullOrWhiteSpace(path))
                continue;

            var tenantPath = new[]
            {
                "{tenantId}", options.Value.BasePath.ToString(), path
            }.JoinSegments();
            routeTable.Add(tenantPath);
        }
    }

    public void RemoveRoutes(IEnumerable<StoredTrigger> triggers)
    {
        var paths = Filter(triggers).Select(x => x.GetPayload<HttpEndpointBookmarkStimulus>().Path).ToList();
        routeTable.RemoveRange(paths);
    }

    public void RemoveRoutes(IEnumerable<Bookmark> bookmarks)
    {
        var paths = Filter(bookmarks).Select(x => x.GetPayload<HttpEndpointBookmarkStimulus>().Path).ToList();
        routeTable.RemoveRange(paths);
    }

    private static IEnumerable<StoredTrigger> Filter(IEnumerable<StoredTrigger> triggers)
    {
        var triggerName = ActivityTypeNameHelper.GenerateTypeName<HttpEndpoint>();
        return triggers.Where(x => x.Name == triggerName && x.Payload != null);
    }

    private static IEnumerable<StoredBookmark> Filter(IEnumerable<StoredBookmark> bookmarks)
    {
        var activityTypeName = ActivityTypeNameHelper.GenerateTypeName<HttpEndpoint>();
        return bookmarks.Where(x => x.ActivityTypeName == activityTypeName && x.Payload != null);
    }

    private static IEnumerable<Bookmark> Filter(IEnumerable<Bookmark> bookmarks)
    {
        var bookmarkName = ActivityTypeNameHelper.GenerateTypeName<HttpEndpoint>();
        return bookmarks.Where(x => x.Name == bookmarkName && x.Payload != null);
    }
}