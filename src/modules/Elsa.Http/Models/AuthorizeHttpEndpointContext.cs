using Elsa.Models;
using Microsoft.AspNetCore.Http;

namespace Elsa.Http.Models
{
    public record AuthorizeHttpEndpointContext(ExpressionExecutionContext ExpressionExecutionContext, HttpContext HttpContext, HttpEndpoint Activity, string WorkflowInstanceId);
}