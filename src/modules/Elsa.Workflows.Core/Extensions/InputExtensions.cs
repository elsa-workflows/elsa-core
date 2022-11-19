using System.Runtime.CompilerServices;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core;

public static class InputExtensions
{
    public static T? TryGet<T>(this Input<T>? input, ActivityExecutionContext context) => context.Get(input);
    public static T? TryGet<T>(this Input<T>? input, ExpressionExecutionContext context) => context.Get(input);
    public static T Get<T>(this Input<T>? input, ActivityExecutionContext context, [CallerMemberName]string? inputName = default) => context.Get(input) ?? throw new Exception($"{inputName} is required.");
    public static T Get<T>(this Input<T>? input, ExpressionExecutionContext context, [CallerMemberName]string? inputName = default) => context.Get(input) ?? throw new Exception($"{inputName} is required.");
}