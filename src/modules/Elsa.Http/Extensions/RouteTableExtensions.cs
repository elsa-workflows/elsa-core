using System.Text.Json;
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
    private static readonly JsonSerializerOptions SerializerOptions;

    static RouteTableExtensions()
    {
        SerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }
    
    /// <summary>
    /// Adds routes from the specified set of triggers.
    /// </summary>
    public static void AddRoutes(this IRouteTable routeTable, IEnumerable<StoredTrigger> triggers)
    {
        var paths = Filter(triggers).Select(x => x.GetData<HttpEndpointBookmarkPayload>().Path).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
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
        var paths = Filter(triggers).Select(x => x.GetData<HttpEndpointBookmarkPayload>().Path).ToList();
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

    private static IEnumerable<StoredTrigger> Filter(IEnumerable<StoredTrigger> triggers) => triggers.Where(x => x.Name == ActivityTypeNameHelper.GenerateTypeName<HttpEndpoint>() && x.Payload != null);
    private static IEnumerable<Bookmark> Filter(IEnumerable<Bookmark> triggers) => triggers.Where(x => x.Name == ActivityTypeNameHelper.GenerateTypeName<HttpEndpoint>() && x.Payload != null);
    // private static HttpEndpointBookmarkPayload Deserialize(StoredTrigger trigger) => Deserialize(trigger.Data!);
    // private static HttpEndpointBookmarkPayload Deserialize(Bookmark bookmark) => Deserialize(bookmark.Data!);
    // private static HttpEndpointBookmarkPayload Deserialize(string model) => JsonSerializer.Deserialize<HttpEndpointBookmarkPayload>(model, SerializerOptions)!;
}