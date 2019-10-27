using System.Net;
using System.Threading.Tasks;
using Elsa.Activities.Http.Services;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.RequestHandlers.Results
{
    public class NotFoundResult : IRequestHandlerResult
    {
        public Task ExecuteResultAsync(HttpContext httpContext, RequestDelegate next)
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
            return Task.CompletedTask;
        }
    }
}