using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Telnyx.Services
{
    internal interface IWebhookHandler
    {
        Task HandleAsync(HttpContext httpContext);
    }
}