using System.Collections.Generic;
using System.Linq;
using Elsa.Activities.Http.Bookmarks;
using Elsa.Activities.Http.Contracts;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Bookmarks;
using Rebus.Extensions;

namespace Elsa.Activities.Http.Extensions;

public static class RouteTableExtensions
{
    private static readonly BookmarkSerializer BookmarkSerializer = new();
    
    public static void AddRoutes(this IRouteTable routeTable, IEnumerable<Trigger> triggers)
    {
        var paths = Filter(triggers).Select(Deserialize).Select(x => x.Path).ToList();
        routeTable.AddRange(paths);
    }

    public static void AddRoutes(this IRouteTable routeTable, IEnumerable<Bookmark> bookmarks)
    {
        var paths = Filter(bookmarks).Select(Deserialize).Select(x => x.Path).ToList();
        routeTable.AddRange(paths);
    }

    public static void RemoveRoutes(this IRouteTable routeTable, IEnumerable<Trigger> triggers)
    {
        var paths = Filter(triggers).Select(Deserialize).Select(x => x.Path).ToList();
        routeTable.RemoveRange(paths);
    }

    public static void RemoveRoutes(this IRouteTable routeTable, IEnumerable<Bookmark> bookmarks)
    {
        var paths = Filter(bookmarks).Select(Deserialize).Select(x => x.Path).ToList();
        routeTable.RemoveRange(paths);
    }

    private static IEnumerable<Trigger> Filter(IEnumerable<Trigger> triggers) => triggers.Where(x => x.ModelType == typeof(HttpEndpointBookmark).GetSimpleAssemblyQualifiedName());
    private static IEnumerable<Bookmark> Filter(IEnumerable<Bookmark> triggers) => triggers.Where(x => x.ModelType == typeof(HttpEndpointBookmark).GetSimpleAssemblyQualifiedName());
    private static HttpEndpointBookmark Deserialize(Trigger trigger) => Deserialize(trigger.Model);
    private static HttpEndpointBookmark Deserialize(Bookmark bookmark) => Deserialize(bookmark.Model);
    private static HttpEndpointBookmark Deserialize(string model) => BookmarkSerializer.Deserialize<HttpEndpointBookmark>(model);
}