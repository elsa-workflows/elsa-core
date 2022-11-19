using Elsa.Expressions.Models;

namespace Elsa.Http.Models;

public record HttpWorkflowResource(ExpressionExecutionContext ExpressionExecutionContext, HttpEndpoint Activity, string WorkflowInstance);