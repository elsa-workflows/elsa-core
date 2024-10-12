using Elsa.Http;
using Elsa.Http.Bookmarks;
using Elsa.Http.Contracts;
using Elsa.Http.Options;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Entities;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Adds extensions to <see cref="IRouteTable"/>.
/// </summary>
public static class RouteTableExtensions
{
    /// <summary>
    /// Adds routes from the specified set of triggers.
    /// </summary>
    public static void AddRoutes(this IRouteTable routeTable, IEnumerable<StoredTrigger> triggers, IOptions<HttpActivityOptions> options)
    {
        var httpEndpointTriggers = Filter(triggers).ToList();

        foreach (var trigger in httpEndpointTriggers)
        {
            var path = trigger.GetPayload<HttpEndpointBookmarkPayload>().Path;

            if (string.IsNullOrWhiteSpace(path))
                continue;

            var route = trigger.TenantId != null
                ? new[]
                {
                    "{tenantId}", options.Value.BasePath.ToString(), path
                }.Join()
                : path;

            routeTable.Add(route);
        }
    }

    /// <summary>
    /// Adds routes from the specified set of bookmarks.
    /// </summary>
    public static void AddRoutes(this IRouteTable routeTable, IEnumerable<StoredBookmark> bookmarks)
    {
        var httpEndpointBookmarks = Filter(bookmarks).ToList();

        foreach (var bookmark in httpEndpointBookmarks)
        {
            var path = bookmark.GetPayload<HttpEndpointBookmarkPayload>().Path;

            if (string.IsNullOrWhiteSpace(path))
                continue;

            var tenantPath = new[]
            {
                bookmark.TenantId, path
            }.Join();
            routeTable.Add(tenantPath);
        }
    }

    /// <summary>
    /// Adds routes from the specified set of bookmarks.
    /// </summary>
    public static void AddRoutes(this IRouteTable routeTable, IEnumerable<Bookmark> bookmarks, string? tenantId)
    {
        var httpEndpointBookmarks = Filter(bookmarks).ToList();

        foreach (var bookmark in httpEndpointBookmarks)
        {
            var path = bookmark.GetPayload<HttpEndpointBookmarkPayload>().Path;

            if (string.IsNullOrWhiteSpace(path))
                continue;

            var tenantPath = new[]
            {
                tenantId, path
            }.Join();
            routeTable.Add(tenantPath);
        }
    }

    /// <summary>
    /// Removes routes from the specified set of triggers.
    /// </summary>
    public static void RemoveRoutes(this IRouteTable routeTable, IEnumerable<StoredTrigger> triggers)
    {
        var paths = Filter(triggers).Select(x => x.GetPayload<HttpEndpointBookmarkPayload>().Path).ToList();
        routeTable.RemoveRange(paths);
    }

    /// <summary>
    /// Removes routes from the specified set of bookmarks.
    /// </summary>
    public static void RemoveRoutes(this IRouteTable routeTable, IEnumerable<Bookmark> bookmarks)
    {
        var paths = Filter(bookmarks).Select(x => x.GetPayload<HttpEndpointBookmarkPayload>().Path).ToList();
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