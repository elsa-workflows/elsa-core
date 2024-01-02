using System.Text.Json.Serialization;
using Elsa.Expressions.Models;
using Elsa.Workflows.Memory;

namespace Elsa.Workflows.Models;

/// <summary>
/// A base type for the <see cref="Input{T}"/> type.
/// </summary>
public abstract class Input : Argument
{
    /// <inheritdoc />
    protected Input(MemoryBlockReference memoryBlockReference, Type type) : base(memoryBlockReference)
    {
        Type = type;
    }

    /// <inheritdoc />
    protected Input(Expression? expression, MemoryBlockReference memoryBlockReference, Type type) : base(memoryBlockReference)
    {
        Expression = expression;
        Type = type;
    }

    /// <summary>
    /// Gets or sets the expression.
    /// </summary>
    public Expression? Expression { get; }

    /// <summary>
    /// Gets the type of the input.
    /// </summary>
    [JsonPropertyName("typeName")]
    public Type Type { get; set; }
}

/// <summary>
/// Represents activity input that is evaluated at runtime.
/// </summary>
public class Input<T> : Input
{
    /// <inheritdoc />
    public Input(MemoryBlockReference memoryBlockReference) : base(memoryBlockReference, typeof(T))
    {
    }

    /// <inheritdoc />
    public Input(T literal, string? id = default) : this(new Literal<T>(literal, id))
    {
    }

    /// <inheritdoc />
    public Input(Func<T> @delegate, string? id = default) : this(Expression.DelegateExpression(@delegate), new MemoryBlockReference(id!))
    {
    }

    /// <inheritdoc />
    public Input(Func<ExpressionExecutionContext, ValueTask<T?>> @delegate, string? id = default) : this(Expression.DelegateExpression(@delegate), new MemoryBlockReference(id!))
    {
    }

    /// <inheritdoc />
    public Input(Func<ValueTask<T?>> @delegate, string? id = default) : this(Expression.DelegateExpression(@delegate), new MemoryBlockReference(id!))
    {
    }

    /// <inheritdoc />
    public Input(Func<ExpressionExecutionContext, T> @delegate, string? id = default) : this(Expression.DelegateExpression(@delegate), new MemoryBlockReference(id!))
    {
    }

    /// <inheritdoc />
    public Input(Variable variable) : base(new Expression("Variable", variable), variable, typeof(T))
    {
    }

    /// <inheritdoc />
    public Input(Output output) : base(new Expression("Output", output), output.MemoryBlockReference(), typeof(T))
    {
    }

    /// <inheritdoc />
    public Input(Literal<T> literal) : base(Expression.LiteralExpression(literal.Value), literal, typeof(T))
    {
    }

    /// <inheritdoc />
    public Input(Literal literal) : base(Expression.LiteralExpression(literal.Value), literal, typeof(T))
    {
    }

    /// <inheritdoc />
    public Input(ObjectLiteral<T> literal) : base(Expression.LiteralExpression(literal.Value), literal, typeof(T))
    {
    }

    /// <inheritdoc />
    public Input(ObjectLiteral literal) : base(Expression.LiteralExpression(literal.Value), literal, typeof(T))
    {
    }

    /// <inheritdoc />
    public Input(Expression expression, MemoryBlockReference memoryBlockReference) : base(expression, memoryBlockReference, typeof(T))
    {
    }

    /// <inheritdoc />
    public Input(Expression expression) : this(expression, new MemoryBlockReference())
    {
    }
}