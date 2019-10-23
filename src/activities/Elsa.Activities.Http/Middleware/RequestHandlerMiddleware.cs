using System.Threading.Tasks;
using Elsa.Activities.Http.Services;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.Middleware
{
    public class RequestHandlerMiddleware<THandler> where THandler : IRequestHandler
    {
        private readonly RequestDelegate next;

        public RequestHandlerMiddleware(RequestDelegate next)
        {
            this.next = next;
        }
        
        public async Task InvokeAsync(HttpContext httpContext, THandler handler)
        {
            var result = await handler.HandleRequestAsync();

            if(result != null && !httpContext.Response.HasStarted)
                await result.ExecuteResultAsync(httpContext, next);
        }
    }
}