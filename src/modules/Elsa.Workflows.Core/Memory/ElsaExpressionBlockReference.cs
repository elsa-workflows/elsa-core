using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Expressions;

namespace Elsa.Workflows.Core.Memory;

/// <summary>
/// Represents an Elsa DSL expression that references a memory block.
/// </summary>
public class ElsaExpressionBlockReference : MemoryBlockReference
{
    /// <inheritdoc />
    public ElsaExpressionBlockReference(ElsaExpression expression)
    {
        Expression = expression;
    }
        
    /// <summary>
    /// The expression.
    /// </summary>
    public ElsaExpression Expression { get; }

    /// <inheritdoc />
    public override MemoryBlock Declare() => new();
}