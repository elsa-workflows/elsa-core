using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Elsa.Http.Models;
using Elsa.Http.Services;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Persistence.Entities;

namespace Elsa.Http.Extensions;

public static class RouteTableExtensions
{
    
    public static void AddRoutes(this IRouteTable routeTable, IEnumerable<WorkflowTrigger> triggers)
    {
        var paths = Filter(triggers).Select(Deserialize).Select(x => x.Path).ToList();
        routeTable.AddRange(paths);
    }

    public static void AddRoutes(this IRouteTable routeTable, IEnumerable<WorkflowBookmark> bookmarks)
    {
        var paths = Filter(bookmarks).Select(Deserialize).Select(x => x.Path).ToList();
        routeTable.AddRange(paths);
    }

    public static void RemoveRoutes(this IRouteTable routeTable, IEnumerable<WorkflowTrigger> triggers)
    {
        var paths = Filter(triggers).Select(Deserialize).Select(x => x.Path).ToList();
        routeTable.RemoveRange(paths);
    }

    public static void RemoveRoutes(this IRouteTable routeTable, IEnumerable<WorkflowBookmark> bookmarks)
    {
        var paths = Filter(bookmarks).Select(Deserialize).Select(x => x.Path).ToList();
        routeTable.RemoveRange(paths);
    }

    private static IEnumerable<WorkflowTrigger> Filter(IEnumerable<WorkflowTrigger> triggers) => triggers.Where(x => x.Name == ActivityTypeNameHelper.GenerateTypeName<HttpEndpoint>());
    private static IEnumerable<WorkflowBookmark> Filter(IEnumerable<WorkflowBookmark> triggers) => triggers.Where(x => x.Name == ActivityTypeNameHelper.GenerateTypeName<HttpEndpoint>());
    private static HttpEndpointBookmarkData Deserialize(WorkflowTrigger trigger) => Deserialize(trigger.Data!);
    private static HttpEndpointBookmarkData Deserialize(WorkflowBookmark bookmark) => Deserialize(bookmark.Data!);
    private static HttpEndpointBookmarkData Deserialize(string model) => JsonSerializer.Deserialize<HttpEndpointBookmarkData>(model)!;
}