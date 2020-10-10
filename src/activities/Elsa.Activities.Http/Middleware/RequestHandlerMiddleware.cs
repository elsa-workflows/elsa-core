using System.Threading.Tasks;
using Elsa.Activities.Http.Services;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.Middleware
{
    public class RequestHandlerMiddleware<THandler> where THandler : IRequestHandler
    {
        private readonly RequestDelegate _next;

        public RequestHandlerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext, THandler handler)
        {
            var result = await handler.HandleRequestAsync();

            if (result != null && !httpContext.Response.HasStarted)
                await result.ExecuteResultAsync(httpContext, _next);
        }
    }
}