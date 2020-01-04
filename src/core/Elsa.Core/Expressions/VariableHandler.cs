using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Expressions
{
    public class VariableHandler : IExpressionHandler
    {
        public string Type => VariableExpression.ExpressionType;

        public Task<object> EvaluateAsync(
            IWorkflowExpression expression,
            WorkflowExecutionContext workflowExecutionContext,
            ActivityExecutionContext activityExecutionContext,
            CancellationToken cancellationToken)
        {
            var variableExpression = (VariableExpression)expression;
            var result = workflowExecutionContext.GetVariable(variableExpression.VariableName);
            return Task.FromResult(result);
        }
    }
}