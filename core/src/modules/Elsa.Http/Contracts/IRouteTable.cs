namespace Elsa.Http;

/// <summary>
/// Stores a list of all routes provided by <see cref="HttpEndpoint"/> activities.
/// </summary>
public interface IRouteTable : IEnumerable<HttpRouteData>
{
    /// <summary>
    /// Adds a route to the table.
    /// </summary>
    /// <param name="route">The route to add.</param>
    void Add(string route);

    /// <summary>
    /// Adds a route to the table.
    /// </summary>
    /// <param name="httpRouteData">The route to add.</param>
    void Add(HttpRouteData httpRouteData);

    /// <summary>
    /// Removes a route from the table.
    /// </summary>
    /// <param name="route">The route to remove.</param>
    void Remove(string route);

    /// <summary>
    /// Adds a range of routes to the table.
    /// </summary>
    /// <param name="routes">The routes to add.</param>
    void AddRange(IEnumerable<string> routes);

    /// <summary>
    /// Removes a range of routes from the table.
    /// </summary>
    /// <param name="routes">The routes to remove.</param>
    void RemoveRange(IEnumerable<string> routes);
}