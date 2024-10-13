using System.Collections;
using System.Collections.Concurrent;
using Elsa.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Elsa.Http.Services;

/// <inheritdoc />
public class RouteTable : IRouteTable
{
    private static readonly object Key = new();
    private readonly IMemoryCache _cache;
    private readonly ILogger<RouteTable> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RouteTable"/> class.
    /// </summary>
    public RouteTable(IMemoryCache cache, ILogger<RouteTable> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    private ConcurrentDictionary<string, HttpRouteData> Routes => _cache.GetOrCreate(Key, _ => new ConcurrentDictionary<string, HttpRouteData>())!;

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
            _logger.LogWarning("Path cannot contain double slashes. Ignoring path: {Path}", route);
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