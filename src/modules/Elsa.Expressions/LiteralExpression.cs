using Elsa.Expressions.Contracts;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;

namespace Elsa.Expressions;

/// <summary>
/// Represents a literal expression, whose value is to be used as-is. Unless the destination type is different than the literal value, in which case the value is attempted to be converted.
/// </summary>
public class LiteralExpression : IExpression
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public LiteralExpression()
    {
    }
    
    /// <summary>
    /// Constructor.
    /// </summary>
    public LiteralExpression(object? value) => Value = value;
    
    /// <summary>
    /// The literal value.
    /// </summary>
    public object? Value { get; set; }
}

/// <summary>
/// Represents a literal expression, whose value is to be used as-is. Unless the destination type is different than the literal value, in which case the value is attempted to be converted.
/// </summary>
public class LiteralExpression<T> : LiteralExpression
{
    /// <inheritdoc />
    public LiteralExpression(T? value) : base(value)
    {
    }
}

/// <inheritdoc />
public class LiteralExpressionHandler : IExpressionHandler
{
    private readonly IWellKnownTypeRegistry _wellKnownTypeRegistry;

    /// <summary>
    /// Constructor.
    /// </summary>
    public LiteralExpressionHandler(IWellKnownTypeRegistry wellKnownTypeRegistry)
    {
        _wellKnownTypeRegistry = wellKnownTypeRegistry;
    }
    
    /// <inheritdoc />
    public ValueTask<object?> EvaluateAsync(IExpression expression, Type returnType, ExpressionExecutionContext context)
    {
        var literalExpression = (LiteralExpression)expression;
        var value = literalExpression.Value.ConvertTo(returnType, new ObjectConverterOptions(WellKnownTypeRegistry: _wellKnownTypeRegistry));
        return ValueTask.FromResult(value);
    }
}