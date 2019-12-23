using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Services.Models;

namespace Elsa.Expressions
{
    public class CodeHandler : IWorkflowExpressionHandler
    {
        public string Type => CodeExpression.ExpressionType;

        public Task<object> EvaluateAsync(IWorkflowExpression expression, WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken)
        {
            var codeExpression = (CodeExpression)expression;
            var result = codeExpression.Expression(workflowExecutionContext);
            return Task.FromResult(result);
        }
    }
}