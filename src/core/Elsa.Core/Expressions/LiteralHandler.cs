using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Expressions
{
    public class LiteralHandler : IExpressionHandler
    {
        public string Syntax => SyntaxNames.Literal;

        public Task<object?> EvaluateAsync(
            string expression,
            Type returnType,
            ActivityExecutionContext context,
            CancellationToken cancellationToken) =>
            Task.FromResult(expression.Parse(returnType));
    }
}