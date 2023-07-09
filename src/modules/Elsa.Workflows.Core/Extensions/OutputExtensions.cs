using Elsa.Expressions.Models;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Memory;
using Elsa.Workflows.Core.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides a set of helper extensions to <see cref="Output{T}"/>.
/// </summary>
public static class OutputExtensions
{
    /// <summary>
    /// Creates an input that references the specified output's value.
    /// </summary>
    public static Input<T> CreateInput<T>(this Output output) => new(output);
    
    /// <summary>
    /// Sets the output to the specified value.
    /// </summary>
    public static void Set<T>(this Output<T>? output, ActivityExecutionContext context, T? value) => context.Set(output, value);
    
    /// <summary>
    /// Sets the output to the specified value.
    /// </summary>
    public static void Set<T>(this Output<T>? output, ExpressionExecutionContext context, T? value) => context.Set(output, value);
    
    /// <summary>
    /// Sets the output to the specified value.
    /// </summary>
    public static void Set<T>(this Output<T>? output, ActivityExecutionContext context, Variable<T> value) => context.Set(output, value.Get(context));
    
    /// <summary>
    /// Sets the output to the specified value.
    /// </summary>
    public static void Set<T>(this Output<T>? output, ExpressionExecutionContext context, Variable<T> value) => context.Set(output, value.Get(context));
}