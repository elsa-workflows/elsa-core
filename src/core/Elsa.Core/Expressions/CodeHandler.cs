using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Expressions
{
    public class CodeHandler : IExpressionHandler
    {
        public string Type => CodeExpression.ExpressionType;

        public Task<object> EvaluateAsync(
            IWorkflowExpression expression,
            Type returnType,
            ActivityExecutionContext context,
            CancellationToken cancellationToken)
        {
            var codeExpression = (CodeExpression)expression;
            var result = codeExpression.Expression(context);
            return Task.FromResult(result);
        }
    }
}