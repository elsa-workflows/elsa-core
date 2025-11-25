using System.Runtime.CompilerServices;
using Elsa.Expressions.Models;
using Elsa.Workflows;
using Elsa.Workflows.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extensions on <see cref="ExpressionExecutionContext"/> and <see cref="ActivityExecutionContext"/>
/// </summary>
public static class InputExtensions
{
    /// <param name="input">The input.</param>
    /// <typeparam name="T">The type of the input.</typeparam>
    extension<T>(Input<T>? input)
    {
        /// <summary>
        /// Returns the value of the specified input, or a default value if the input is not found.
        /// </summary>
        public T? GetOrDefault(ActivityExecutionContext context, Func<T>? defaultValue = default)
        {
            var value = context.Get(input);
            return value != null ? value : defaultValue != null ? defaultValue.Invoke() : default;
        }

        /// <summary>
        /// Returns the value of the specified input, or a default value if the input is not found.
        /// </summary>
        public T? GetOrDefault(ExpressionExecutionContext context, Func<T>? defaultValue = default)
        {
            var value = context.Get(input);
            return value != null ? value : defaultValue != null ? defaultValue.Invoke() : default;
        }

        /// <summary>
        /// Returns the value of the specified input.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="inputName">The name of the input.</param>
        /// <returns>The value of the specified input.</returns>
        /// <exception cref="Exception">Throws an exception if the input is not found.</exception>
        public T Get(ActivityExecutionContext context, [CallerArgumentExpression("input")] string? inputName = default)
        {
            return context.Get(input) ?? throw new Exception($"{inputName} is required.");
        }

        /// <summary>
        /// Returns the value of the specified input.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="inputName">The name of the input.</param>
        /// <returns>The value of the specified input.</returns>
        /// <exception cref="Exception">Throws an exception if the input is not found.</exception>
        public T Get(ExpressionExecutionContext context, [CallerArgumentExpression("input")] string? inputName = default)
        {
            return context.Get(input) ?? throw new Exception($"{inputName} is required.");
        }
    }
}