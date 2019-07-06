using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.Services
{
    public interface IRequestHandlerResult
    {
        Task ExecuteResultAsync(HttpContext httpContext, RequestDelegate next);
    }
}