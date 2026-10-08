using System.Net;

namespace Elsa.Http.Extensions;

public static class HttpStatusCodeExtensions
{
    public static bool IsTransientStatusCode(this HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.RequestTimeout => true,
            HttpStatusCode.TooManyRequests => true,
            HttpStatusCode.InternalServerError => true,
            HttpStatusCode.BadGateway => true,
            HttpStatusCode.ServiceUnavailable => true,
            HttpStatusCode.GatewayTimeout => true,
            HttpStatusCode.Conflict => true,
            _ => false
        };
    }
}