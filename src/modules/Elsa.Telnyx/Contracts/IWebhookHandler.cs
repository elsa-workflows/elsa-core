using Microsoft.AspNetCore.Http;

namespace Elsa.Telnyx.Contracts;

internal interface IWebhookHandler
{
    Task HandleAsync(HttpContext httpContext);
}