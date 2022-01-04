using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Elsa.Api.ApiResults;

public class EmptyResult : IResult
{
    public Task ExecuteAsync(HttpContext httpContext) => Task.CompletedTask;
}