using System.Threading;
using Elsa.Models;
using Microsoft.AspNetCore.Http;

namespace Elsa.Modules.Http.Models
{
    public record AuthorizeHttpEndpointContext(ExpressionExecutionContext ExpressionExecutionContext, HttpContext HttpContext, HttpEndpoint Activity, string WorkflowInstanceId);
}