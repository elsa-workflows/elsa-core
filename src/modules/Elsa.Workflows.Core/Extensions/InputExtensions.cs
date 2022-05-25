using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core;

public static class InputExtensions
{
    public static T? Get<T>(this Input<T> input, ActivityExecutionContext context) => context.Get(input);
    public static T? Get<T>(this Input<T> input, ExpressionExecutionContext context) => context.Get(input);
}