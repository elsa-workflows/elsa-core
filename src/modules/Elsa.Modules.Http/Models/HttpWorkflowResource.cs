using Elsa.Models;

namespace Elsa.Modules.Http.Models
{
    public record HttpWorkflowResource(ExpressionExecutionContext ExpressionExecutionContext, HttpEndpoint Activity, string WorkflowInstance);
}