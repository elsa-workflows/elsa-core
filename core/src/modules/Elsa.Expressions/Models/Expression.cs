using System.Text.Json.Serialization;

namespace Elsa.Expressions.Models;

/// <summary>
/// Represents an expression.
/// </summary>
public partial class Expression
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Expression"/> class.
    /// </summary>
    [JsonConstructor]
    public Expression()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Expression"/> class.
    /// </summary>
    /// <param name="type">The expression type.</param>
    /// <param name="value">The expression.</param>
    public Expression(string type, object? value)
    {
        Type = type;
        Value = value;
    }

    /// <summary>
    /// Gets or sets the expression type.
    /// </summary>
    public string Type { get; set; } = default!;

    /// <summary>
    /// Gets or sets the expression.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Creates an expression that represents a literal value.
    /// </summary>
    /// <param name="value">The literal value.</param>
    /// <returns>An expression that represents a literal value.</returns>
    public static Expression LiteralExpression(object? value) => new("Literal", value);

    /// <summary>
    /// Creates an expression that represents a delegate.
    /// </summary>
    /// <param name="value">The delegate.</param>
    /// <returns>An expression that represents a delegate.</returns>
    public static Expression DelegateExpression(Func<ExpressionExecutionContext, ValueTask<object?>> value) => new()
    {
        Type = "Delegate",
        Value = value
    };

    /// <summary>
    /// Creates an expression that represents a delegate.
    /// </summary>
    /// <param name="value">The delegate.</param>
    /// <typeparam name="T">The return type of the delegate.</typeparam>
    /// <returns>An expression that represents a delegate.</returns>
    public static Expression DelegateExpression<T>(Func<ExpressionExecutionContext, ValueTask<T>> value) => DelegateExpression(async context => (object?)await value(context));

    /// <summary>
    /// Creates an expression that represents a delegate.
    /// </summary>
    /// <param name="value">The delegate.</param>
    /// <typeparam name="T">The return type of the delegate.</typeparam>
    /// <returns>An expression that represents a delegate.</returns>
    public static Expression DelegateExpression<T>(Func<ValueTask<T>> value) => DelegateExpression(_ => ValueTask.FromResult<object?>(value()));

    /// <summary>
    /// Creates an expression that represents a delegate.
    /// </summary>
    /// <param name="value">The delegate.</param>
    /// <typeparam name="T">The return type of the delegate.</typeparam>
    /// <returns>An expression that represents a delegate.</returns>
    public static Expression DelegateExpression<T>(Func<ExpressionExecutionContext, T> value) => DelegateExpression(context => ValueTask.FromResult<object?>(value(context)));

    /// <summary>
    /// Creates an expression that represents a delegate.
    /// </summary>
    /// <param name="value">The delegate.</param>
    /// <typeparam name="T">The return type of the delegate.</typeparam>
    /// <returns>An expression that represents a delegate.</returns>
    public static Expression DelegateExpression<T>(Func<T> value) => DelegateExpression(_ => ValueTask.FromResult<object?>(value()));
}