using System.Collections.Generic;

namespace Elsa.Http.Services;

/// <summary>
/// Stores a list of all routes provided by <see cref="HttpEndpoint"/> activities.
/// </summary>
public interface IRouteTable : IEnumerable<string>
{
    void Add(string path);
    void Remove(string path);
    void AddRange(IEnumerable<string> paths);
    void RemoveRange(IEnumerable<string> paths);
}