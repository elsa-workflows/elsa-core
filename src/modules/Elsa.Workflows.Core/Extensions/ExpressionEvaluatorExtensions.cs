using System.Threading.Tasks;
using Elsa.Expressions.Models;
using Elsa.Expressions.Services;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core;

public static class ExpressionEvaluatorExtensions
{
    public static ValueTask<T?> EvaluateAsync<T>(this IExpressionEvaluator evaluator, Input<T> input, ExpressionExecutionContext context) => evaluator.EvaluateAsync<T>(input.Expression, context);
    public static ValueTask<object?> EvaluateAsync(this IExpressionEvaluator evaluator, Input input, ExpressionExecutionContext context) => evaluator.EvaluateAsync(input.Expression, input.Type, context);
}