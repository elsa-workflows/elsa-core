using System.Text.Json.Serialization;
using Elsa.Expressions;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Expressions;

namespace Elsa.Workflows.Core.Models;

/// <summary>
/// A base type for the <see cref="Input{T}"/> type.
/// </summary>
public abstract class Input : Argument
{
    /// <inheritdoc />
    protected Input(IExpression expression, MemoryBlockReference memoryBlockReference, Type type) : base(memoryBlockReference)
    {
        Expression = expression;
        Type = type;
    }

    public IExpression Expression { get; }

    [JsonPropertyName("typeName")] public Type Type { get; set; }
}

/// <summary>
/// Represents activity input that is evaluated at runtime.
/// </summary>
public class Input<T> : Input
{
    /// <inheritdoc />
    public Input(T literal, string? id = default) : this(new Literal<T>(literal) { Id = id! })
    {
    }

    /// <inheritdoc />
    public Input(Func<T> @delegate, string? id = default) : this(new DelegateBlockReference(() => @delegate()){ Id = id!})
    {
    }

    /// <inheritdoc />
    public Input(Func<ExpressionExecutionContext, ValueTask<T?>> @delegate) : this(new DelegateBlockReference<T>(@delegate))
    {
    }

    /// <inheritdoc />
    public Input(Func<ValueTask<T?>> @delegate) : this(new DelegateBlockReference<T>(@delegate))
    {
    }

    /// <inheritdoc />
    public Input(Func<ExpressionExecutionContext, T> @delegate) : this(new DelegateBlockReference<T>(@delegate))
    {
    }

    /// <inheritdoc />
    public Input(Variable variable) : base(new VariableExpression(variable), variable, typeof(T))
    {
    }

    /// <inheritdoc />
    public Input(Output output) : base(new OutputExpression(output), output.MemoryBlockReference(), typeof(T))
    {
    }

    /// <inheritdoc />
    public Input(Literal<T> literal) : base(new LiteralExpression(literal.Value), literal, typeof(T))
    {
    }

    /// <inheritdoc />
    public Input(Literal literal) : base(new LiteralExpression(literal.Value), literal, typeof(T))
    {
    }
    
    /// <inheritdoc />
    public Input(ObjectLiteral<T> literal) : base(new ObjectExpression(literal.Value), literal, typeof(T))
    {
    }

    /// <inheritdoc />
    public Input(ObjectLiteral literal) : base(new ObjectExpression(literal.Value), literal, typeof(T))
    {
    }

    /// <inheritdoc />
    public Input(DelegateBlockReference delegateBlockReference) : base(new DelegateExpression(delegateBlockReference), delegateBlockReference, typeof(T))
    {
    }

    /// <inheritdoc />
    public Input(ElsaExpression expression) : this(new ElsaExpressionBlockReference(expression))
    {
    }

    /// <inheritdoc />
    public Input(IExpression expression, MemoryBlockReference memoryBlockReference) : base(expression, memoryBlockReference, typeof(T))
    {
    }

    private Input(ElsaExpressionBlockReference expressionBlockReference) : base(expressionBlockReference.Expression, expressionBlockReference, typeof(T))
    {
    }
}