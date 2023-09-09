using System.Collections;
using System.Collections.Concurrent;
using Elsa.Http.Contracts;
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

    private ConcurrentDictionary<string, string> Routes => _cache.GetOrCreate(Key, _ => new ConcurrentDictionary<string, string>())!;

    /// <inheritdoc />
    public void Add(string path)
    {
        if (path.Contains("//"))
        {
            _logger.LogWarning("Path cannot contain double slashes. Ignoring path: {Path}", path);
            return;
        }

        Routes.TryAdd(path, path);
    }

    /// <inheritdoc />
    public void Remove(string path) => Routes.TryRemove(path, out _);

    /// <inheritdoc />
    public void AddRange(IEnumerable<string> paths)
    {
        foreach (var path in paths) Add(path);
    }

    /// <inheritdoc />
    public void RemoveRange(IEnumerable<string> paths)
    {
        foreach (var path in paths) Remove(path);
    }

    /// <inheritdoc />
    public IEnumerator<string> GetEnumerator() => Routes.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}