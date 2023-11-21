using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Memory;
using Elsa.Workflows.Core.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

internal static class OutputExtensions
{
    /// <summary>
    /// Gets the target type of the specified variable type.
    /// </summary>
    public static Type? GetTargetType(this Output? output, ActivityExecutionContext context)
    {
        var memoryBlockReference = output?.MemoryBlockReference();
        
        if (memoryBlockReference is null)
            return default;

        if(!context.ExpressionExecutionContext.TryGetBlock(memoryBlockReference, out var memoryBlock))
            return default;
        
        var parsedContentVariableType = (memoryBlock.Metadata as VariableBlockMetadata)?.Variable.GetType();
        return parsedContentVariableType?.GenericTypeArguments.FirstOrDefault();
    }
}