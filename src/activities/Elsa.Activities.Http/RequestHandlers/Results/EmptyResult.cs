using System.Threading.Tasks;
using Elsa.Activities.Http.Services;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.RequestHandlers.Results
{
    public class EmptyResult : IRequestHandlerResult
    {
        public Task ExecuteResultAsync(HttpContext httpContext, RequestDelegate next) => Task.CompletedTask;
    }
}