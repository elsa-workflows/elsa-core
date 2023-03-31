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
    public static object? Get(this MemoryBlockReference block, ActivityExecutionContext context)
    {
        var matchingContext = context.ExpressionExecutionContext.FindContextContainingBlock(block.Id) ?? context.ExpressionExecutionContext;
        return block.Get(matchingContext);
    }
    
    public static MemoryBlock GetBlock(this MemoryBlockReference block, ExpressionExecutionContext context)
    {
        var matchingContext = context.FindContextContainingBlock(block.Id) ?? context;
        return block.GetBlock(matchingContext.Memory);
    }

    public static T? Get<T>(this MemoryBlockReference block, ActivityExecutionContext context) => block.Get(context).ConvertTo<T>();
    
    public static void Set(this MemoryBlockReference block, ActivityExecutionContext context, object? value)
    {
        var matchingContext = context.ExpressionExecutionContext.FindContextContainingBlock(block.Id) ?? context.ExpressionExecutionContext;
        block.Set(matchingContext, value);
    }
}