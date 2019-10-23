using System.Net;
using Microsoft.AspNetCore.Http;

namespace Elsa.Dashboard.Extensions
{
    // Taken & adapted from https://www.strathweb.com/2016/04/request-islocal-in-asp-net-core/
    public static class HttpRequestExtensions
    {
        public static bool IsLocal(this HttpRequest request)
        {
            var connection = request.HttpContext.Connection;
            if (connection.RemoteIpAddress != null)
            {
                return connection.LocalIpAddress != null 
                    ? connection.RemoteIpAddress.Equals(connection.LocalIpAddress) 
                    : IPAddress.IsLoopback(connection.RemoteIpAddress);
            }

            // For in memory TestServer or when dealing with default connection info.
            return connection.RemoteIpAddress == null && connection.LocalIpAddress == null;
        }
    }
}