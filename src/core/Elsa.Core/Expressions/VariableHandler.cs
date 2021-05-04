using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Expressions
{
    public class VariableHandler : IExpressionHandler
    {
        public string Syntax => SyntaxNames.Variable;

        public Task<object?> EvaluateAsync(
            string expression,
            Type returnType,
            ActivityExecutionContext context,
            CancellationToken cancellationToken)
        {
            var result = context.GetVariable(expression);
            return Task.FromResult(result);
        }
    }
}