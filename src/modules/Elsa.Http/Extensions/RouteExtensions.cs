// ReSharper disable once CheckNamespace

namespace Elsa.Extensions;

/// <summary>
/// Provides extension methods for strings that represent a route.
/// </summary>
public static class RouteExtensions
{
    /// <summary>
    /// Normalizes a route by ensuring a leading slash, removing any trailing slash and converting the path to lowercase.
    /// </summary>
    public static string NormalizeRoute(this string path) => $"/{path.Trim('/').ToLowerInvariant()}";
}