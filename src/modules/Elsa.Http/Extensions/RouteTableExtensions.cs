using Elsa.Http;
using Elsa.Http.Contracts;
using Elsa.Http.Models;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Runtime.Entities;

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
    public static void AddRoutes(this IRouteTable routeTable, IEnumerable<StoredTrigger> triggers)
    {
        var paths = Filter(triggers).Select(x => x.GetPayload<HttpEndpointBookmarkPayload>().Path).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        routeTable.AddRange(paths);
    }

    /// <summary>
    /// Adds routes from the specified set of bookmarks.
    /// </summary>
    public static void AddRoutes(this IRouteTable routeTable, IEnumerable<StoredBookmark> bookmarks)
    {
        var paths = Filter(bookmarks).Select(x => x.GetPayload<HttpEndpointBookmarkPayload>().Path).ToList();
        routeTable.AddRange(paths);
    }
    
    /// <summary>
    /// Adds routes from the specified set of bookmarks.
    /// </summary>
    public static void AddRoutes(this IRouteTable routeTable, IEnumerable<Bookmark> bookmarks)
    {
        var paths = Filter(bookmarks).Select(x => x.GetPayload<HttpEndpointBookmarkPayload>().Path).ToList();
        routeTable.AddRange(paths);
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