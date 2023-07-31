using System.Text.Json;
using Elsa.Expressions.Contracts;

namespace Elsa.Expressions.Models;

/// <summary>
/// Describes an expression syntax.
/// </summary>
public class ExpressionSyntaxDescriptor
{
    /// <summary>
    /// Gets or sets the syntax name.
    /// </summary>
    public string Syntax { get; init; } = default!;
    
    /// <summary>
    /// Gets or sets the expression type.
    /// </summary>
    public Type Type { get; init; } = default!;
    
    /// <summary>
    /// Gets or sets a delegate that creates an expression.
    /// </summary>
    public Func<ExpressionConstructorContext, IExpression> CreateExpression { get; init; } = default!;
    
    /// <summary>
    /// Gets or sets a delegate that creates a memory block reference.
    /// </summary>
    public Func<BlockReferenceConstructorContext, MemoryBlockReference> CreateBlockReference { get; init; } = default!;
    
    /// <summary>
    /// Gets or sets a delegate that creates a serializable object.
    /// </summary>
    public Func<SerializableObjectConstructorContext, object> CreateSerializableObject { get; init; } = default!;
}

/// <summary>
/// Contextual information for creating an expression.
/// </summary>
/// <param name="Element">The JSON element containing the expression.</param>
/// <param name="SerializerOptions">The JSON serializer options.</param>
public record ExpressionConstructorContext(JsonElement Element, JsonSerializerOptions SerializerOptions);

/// <summary>
/// Contextual information for creating a memory block reference.
/// </summary>
/// <param name="Expression">The expression.</param>
public record BlockReferenceConstructorContext(IExpression Expression)
{
    /// <summary>
    /// Gets the expression.
    /// </summary>
    /// <typeparam name="T">The expression type.</typeparam>
    /// <returns>The expression.</returns>
    public T GetExpression<T>() => (T)Expression;
}

/// <summary>
/// Contextual information for creating a serializable object.
/// </summary>
/// <param name="Expression">The expression.</param>
public record SerializableObjectConstructorContext(IExpression Expression)
{
    /// <summary>
    /// Gets the expression.
    /// </summary>
    /// <typeparam name="T">The expression type.</typeparam>
    /// <returns>The expression.</returns>
    public T GetExpression<T>() => (T)Expression;
}