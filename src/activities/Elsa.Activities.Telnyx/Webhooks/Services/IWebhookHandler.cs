using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Webhooks.Attributes;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Telnyx.Webhooks.Services
{
    internal interface IWebhookHandler
    {
        Task HandleAsync(HttpContext httpContext);
    }
}