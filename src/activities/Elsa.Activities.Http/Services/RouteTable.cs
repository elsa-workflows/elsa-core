using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Elsa.Activities.Http.Contracts;
using Microsoft.Extensions.Caching.Memory;

namespace Elsa.Activities.Http.Services;

public class RouteTable : IRouteTable
{
    private static readonly object Key = new();
    private readonly IMemoryCache _cache;
    
    public RouteTable(IMemoryCache cache) => _cache = cache;
    private ConcurrentDictionary<string, string> Routes => _cache.GetOrCreate(Key, _ => new ConcurrentDictionary<string, string>());
    public void Add(string path) => Routes.TryAdd(path, path);
    public void Remove(string path) => Routes.TryRemove(path, out _);

    public void AddRange(IEnumerable<string> paths)
    {
        foreach (var path in paths) Add(path);
    }

    public void RemoveRange(IEnumerable<string> paths)
    {
        foreach (var path in paths) Remove(path);
    }

    public IEnumerator<string> GetEnumerator() => Routes.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}