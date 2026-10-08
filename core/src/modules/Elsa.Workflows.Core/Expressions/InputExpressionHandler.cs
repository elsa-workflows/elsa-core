using System.Diagnostics;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Expressions;

/// <summary>
/// Handles Input expressions.
/// </summary>
public class InputExpressionHandler : IExpressionHandler
{
    /// <inheritdoc />
    public ValueTask<object?> EvaluateAsync(Expression expression, Type returnType, ExpressionExecutionContext context, ExpressionEvaluatorOptions options)
    {
        object? result = null;
        var inputDefinition = expression.Value as InputDefinition;

        if (inputDefinition != null)
        {
            result = context.GetInput(inputDefinition.Name);
        }

        return ValueTask.FromResult(result);
    }
}