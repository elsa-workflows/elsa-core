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
        var memoryBlock = output?.MemoryBlockReference() is Variable variable
            ? context.WorkflowExecutionContext.MemoryRegister.TryGetBlock(variable.Id, out var block)
                ? block
                : default
            : default;

        var parsedContentVariableType = (memoryBlock?.Metadata as VariableBlockMetadata)?.Variable.GetType();
        return parsedContentVariableType?.GenericTypeArguments.FirstOrDefault();
    }
}