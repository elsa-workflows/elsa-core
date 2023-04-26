using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extension methods for <see cref="MemoryBlockReference"/>.
/// </summary>
public static class MemoryBlockReferenceExtensions
{
    /// <summary>
    /// Gets the value of the memory reference referenced by the specified <see cref="MemoryBlockReference"/>.
    /// </summary>
    /// <param name="reference">The <see cref="MemoryBlockReference"/> to get the value for.</param>
    /// <param name="context">The <see cref="ActivityExecutionContext"/> to get the value from.</param>
    /// <returns>The value of the memory reference referenced by the specified <see cref="MemoryBlockReference"/>.</returns>
    public static object? Get(this MemoryBlockReference reference, ActivityExecutionContext context)
    {
        var matchingContext = context.ExpressionExecutionContext.FindContextContainingBlock(reference.Id) ?? context.ExpressionExecutionContext;
        return reference.Get(matchingContext);
    }

    /// <summary>
    /// Gets the <see cref="MemoryBlock"/> of referenced by the specified <see cref="MemoryBlockReference"/>.
    /// </summary>
    /// <param name="reference">The reference to get the reference for.</param>
    /// <param name="context">The <see cref="ExpressionExecutionContext"/> to get the reference from.</param>
    /// <returns>The <see cref="MemoryBlock"/> referenced by the specified <see cref="MemoryBlockReference"/>.</returns>
    public static MemoryBlock GetBlock(this MemoryBlockReference reference, ExpressionExecutionContext context)
    {
        var matchingContext = context.FindContextContainingBlock(reference.Id) ?? context;
        return reference.GetBlock(matchingContext.Memory);
    }

    /// <summary>
    /// Gets the value of the memory reference referenced by the specified <see cref="MemoryBlockReference"/>.
    /// </summary>
    /// <param name="reference">The <see cref="MemoryBlockReference"/> to get the value for.</param>
    /// <param name="context">The <see cref="ActivityExecutionContext"/> to get the value from.</param>
    /// <typeparam name="T">The type of the value to get.</typeparam>
    /// <returns>The value of the memory reference referenced by the specified <see cref="MemoryBlockReference"/>.</returns>
    public static T? Get<T>(this MemoryBlockReference reference, ActivityExecutionContext context) => reference.Get(context).ConvertTo<T>();

    /// <summary>
    /// Sets the specified value on the memory referenced by the specified <see cref="MemoryBlockReference"/>.
    /// If the referenced block doesn't exist in the current scope, an attempt is made to find the block in the first parent scope that references an activity that is a variable container.
    /// If that fails, the root scope is used.
    /// </summary>
    /// <param name="reference">The <see cref="MemoryBlockReference"/> to set the value for.</param>
    /// <param name="context">The <see cref="ActivityExecutionContext"/> to set the value on.</param>
    /// <param name="value">The value to set.</param>
    public static void Set(this MemoryBlockReference reference, ActivityExecutionContext context, object? value)
    {
        var matchingContext =
            context.ExpressionExecutionContext.FindContextContainingBlock(reference.Id) 
            ?? context.FindParentWithVariableContainer()?.ExpressionExecutionContext 
            ?? context.WorkflowExecutionContext.ExpressionExecutionContext!;

        reference.Set(matchingContext, value);
    }
}