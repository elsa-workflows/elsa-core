using System.Runtime.CompilerServices;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extensions on <see cref="ExpressionExecutionContext"/> and <see cref="ActivityExecutionContext"/>
/// </summary>
public static class InputExtensions
{
    /// <summary>
    /// Returns the value of the specified input, or a default value if the input is not found.
    /// </summary>
    public static T? GetOrDefault<T>(this Input<T>? input, ActivityExecutionContext context, Func<T>? defaultValue = default)
    {
        var value = context.Get(input);
        return value != null ? value : defaultValue != null ? defaultValue.Invoke() : default;
    }

    /// <summary>
    /// Returns the value of the specified input, or a default value if the input is not found.
    /// </summary>
    public static T? GetOrDefault<T>(this Input<T>? input, ExpressionExecutionContext context, Func<T>? defaultValue = default)
    {
        var value = context.Get(input);
        return value != null ? value : defaultValue != null ? defaultValue.Invoke() : default;
    }

    /// <summary>
    /// Returns the value of the specified input.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <param name="context">The context.</param>
    /// <param name="inputName">The name of the input.</param>
    /// <typeparam name="T">The type of the input.</typeparam>
    /// <returns>The value of the specified input.</returns>
    /// <exception cref="Exception">Throws an exception if the input is not found.</exception>
    public static T Get<T>(this Input<T>? input, ActivityExecutionContext context, [CallerArgumentExpression("input")] string? inputName = default)
    {
        return context.Get(input) ?? throw new Exception($"{inputName} is required.");
    }

    /// <summary>
    /// Returns the value of the specified input.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <param name="context">The context.</param>
    /// <param name="inputName">The name of the input.</param>
    /// <typeparam name="T">The type of the input.</typeparam>
    /// <returns>The value of the specified input.</returns>
    /// <exception cref="Exception">Throws an exception if the input is not found.</exception>
    public static T Get<T>(this Input<T>? input, ExpressionExecutionContext context, [CallerArgumentExpression("input")] string? inputName = default)
    {
        return context.Get(input) ?? throw new Exception($"{inputName} is required.");
    }
}