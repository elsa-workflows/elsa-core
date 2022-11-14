using Microsoft.AspNetCore.Http;

namespace Elsa.Telnyx.Services
{
    internal interface IWebhookHandler
    {
        Task HandleAsync(HttpContext httpContext);
    }
}