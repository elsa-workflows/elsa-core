using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Services.Models;

namespace Elsa.Expressions
{
    public class LiteralHandler : IExpressionHandler
    {
        public string Type => LiteralExpression.ExpressionType;

        public Task<object> EvaluateAsync(
            IWorkflowExpression expression,
            WorkflowExecutionContext workflowExecutionContext,
            ActivityExecutionContext activityExecutionContext,
            CancellationToken cancellationToken)
        {
            var literalExpression = (LiteralExpression)expression;
            if (string.IsNullOrWhiteSpace(literalExpression.Expression))
                return Task.FromResult(default(object));
            
            return Task.FromResult(literalExpression.Expression.Parse(expression.ReturnType));
        }
    }
}