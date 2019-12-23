using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Expressions
{
    public class CodeHandler : IWorkflowExpressionHandler
    {
        public string Type => CodeExpression.ExpressionType;

        public Task<object> EvaluateAsync(
            IWorkflowExpression expression,
            ActivityExecutionContext context,
            CancellationToken cancellationToken)
        {
            var codeExpression = (CodeExpression)expression;
            var result = codeExpression.Expression(context);
            return Task.FromResult(result);
        }
    }
}