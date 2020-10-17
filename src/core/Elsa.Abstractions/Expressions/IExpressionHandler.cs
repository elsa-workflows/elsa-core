using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Expressions
{
    public interface IExpressionHandler
    {
        string Syntax { get; }
        Task<object?> EvaluateAsync(
            string expression,
            Type returnType,
            ActivityExecutionContext context,
            CancellationToken cancellationToken);
    }
}