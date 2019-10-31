using System.Net;
using System.Threading.Tasks;
using Elsa.Activities.Http.Services;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.RequestHandlers.Results
{
    public class BadRequestResult : IRequestHandlerResult
    {
        public BadRequestResult()
        {
        }

        public BadRequestResult(string message)
        {
            Message = message;
        }
        
        public string Message { get; }

        
        public async Task ExecuteResultAsync(HttpContext httpContext, RequestDelegate next)
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            
            if(!string.IsNullOrWhiteSpace(Message))
                await httpContext.Response.WriteAsync(Message, httpContext.RequestAborted);
        }
    }
}