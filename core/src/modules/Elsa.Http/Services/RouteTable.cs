using System.Collections;
using System.Collections.Concurrent;
using Elsa.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Elsa.Http.Services;

/// <inheritdoc />
public class RouteTable(IMemoryCache cache, ILogger<RouteTable> logger) : IRouteTable
{
    private static readonly object Key = new();

    private ConcurrentDictionary<string, HttpRouteData> Routes => cache.GetOrCreate(Key, _ => new ConcurrentDictionary<string, HttpRouteData>())!;

    /// <inheritdoc />
    public void Add(string route)
    {
        Add(new HttpRouteData(route));
    }
    
    /// <inheritdoc />
    public void Add(HttpRouteData httpRouteData)
    {
        var route = httpRouteData.Route;
        if (route.Contains("//"))
        {
            logger.LogWarning("Path cannot contain double slashes. Ignoring path: {Path}", route);
            return;
        }

        var normalizedRoute = route.NormalizeRoute();
        Routes.TryAdd(normalizedRoute, httpRouteData);
    }

    /// <inheritdoc />
    public void Remove(string route)
    {
        var normalizedRoute = route.NormalizeRoute();
        Routes.TryRemove(normalizedRoute, out _);
    }

    /// <inheritdoc />
    public void AddRange(IEnumerable<string> routes)
    {
        foreach (var route in routes) Add(route);
    }

    /// <inheritdoc />
    public void RemoveRange(IEnumerable<string> routes)
    {
        foreach (var route in routes) Remove(route);
    }

    /// <inheritdoc />
    public IEnumerator<HttpRouteData> GetEnumerator() => Routes.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}