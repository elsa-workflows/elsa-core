using System.Collections.Generic;

namespace Elsa.Activities.Http.Contracts;

/// <summary>
/// Stores a list of all routes provided by <see cref="HttpEndpoint"/> activities.
/// </summary>
public interface IRouteTable : IEnumerable<string>
{
    /// <summary>
    /// Adds a route to the table.
    /// Paths containing double slashes are ignored.
    ///
    /// When proper input validation is implemented, this should never happen.
    /// </summary>
    void Add(string path);
    void Remove(string path);
    /// <summary>
    /// Refer to <see cref="Add"/>, concerning double slashes.
    /// </summary>
    void AddRange(IEnumerable<string> paths);
    void RemoveRange(IEnumerable<string> paths);
}