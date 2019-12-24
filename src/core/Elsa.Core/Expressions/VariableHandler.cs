using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Expressions
{
    public class VariableHandler : IWorkflowExpressionHandler
    {
        public string Type => VariableExpression.ExpressionType;

        public Task<object> EvaluateAsync(
            IWorkflowExpression expression,
            ActivityExecutionContext context,
            CancellationToken cancellationToken)
        {
            var variableExpression = (VariableExpression)expression;
            var result = context.GetVariable(variableExpression.VariableName);
            return Task.FromResult(result);
        }
    }
}