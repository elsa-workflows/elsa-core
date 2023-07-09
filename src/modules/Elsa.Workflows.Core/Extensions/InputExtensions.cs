using System.Runtime.CompilerServices;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class InputExtensions
{
    public static T? GetOrDefault<T>(this Input<T>? input, ActivityExecutionContext context) => context.Get(input);
    public static T? GetOrDefault<T>(this Input<T>? input, ExpressionExecutionContext context) => context.Get(input);
    public static T Get<T>(this Input<T>? input, ActivityExecutionContext context, [CallerArgumentExpression("input")]string? inputName = default) => context.Get(input) ?? throw new Exception($"{inputName} is required.");
    public static T Get<T>(this Input<T>? input, ExpressionExecutionContext context, [CallerArgumentExpression("input")]string? inputName = default) => context.Get(input) ?? throw new Exception($"{inputName} is required.");
}