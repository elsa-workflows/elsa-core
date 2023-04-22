using System.Net;
using Microsoft.AspNetCore.Http;

namespace Elsa.Extensions;

/// <summary>
/// Extension methods for <see cref="HttpRequest"/>.
/// </summary>
public static class HttpRequestExtensions
{
    /// <summary>
    /// Returns a value indicating whether the specified request is a local request.
    /// </summary>
    /// <param name="request"></param>
    /// <returns>True if the request is local, otherwise false.</returns>
    public static bool IsLocal(this HttpRequest request)
    {
        var connection = request.HttpContext.Connection;
        return connection.RemoteIpAddress != null
            ? connection.LocalIpAddress != null
                ? connection.RemoteIpAddress.Equals(connection.LocalIpAddress)
                : IPAddress.IsLoopback(connection.RemoteIpAddress)
            : connection.RemoteIpAddress == null && connection.LocalIpAddress == null;
    }
}