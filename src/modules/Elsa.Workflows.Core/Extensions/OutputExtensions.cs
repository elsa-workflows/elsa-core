using System.Runtime.CompilerServices;
using Elsa.Expressions.Models;
using Elsa.Workflows;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides a set of helper extensions to <see cref="Output{T}"/>.
/// </summary>
public static class OutputExtensions
{
    /// <summary>
    /// Creates an input that references the specified output's value.
    /// </summary>
    public static Input<T> CreateInput<T>(this Output output) => new(output);
    
    /// <summary>
    /// Sets the output to the specified value.
    /// </summary>
    public static void Set<T>(this Output<T>? output, ActivityExecutionContext context, T? value, [CallerArgumentExpression("output")] string? outputName = default) => context.Set(output, value, outputName);
    
    /// <summary>
    /// Sets the output to the specified value.
    /// </summary>
    public static void Set<T>(this Output<T>? output, ExpressionExecutionContext context, T? value) => context.Set(output, value);
    
    /// <summary>
    /// Sets the output to the specified value.
    /// </summary>
    public static void Set<T>(this Output<T>? output, ActivityExecutionContext context, Variable<T> value) => context.Set(output, value.Get(context));
    
    /// <summary>
    /// Sets the output to the specified value.
    /// </summary>
    public static void Set<T>(this Output<T>? output, ExpressionExecutionContext context, Variable<T> value) => context.Set(output, value.Get(context));
    
    /// <summary>
    /// Gets the target type of the specified variable type, if any, linked to the output.
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

    /// <summary>
    /// Returns a value indicating whether the output has a target.
    /// </summary>
    public static bool HasTarget(this Output? output, ActivityExecutionContext context)
    {
        var memoryBlockReference = output?.MemoryBlockReference();
        return memoryBlockReference is not null && context.ExpressionExecutionContext.TryGetBlock(memoryBlockReference, out _);
    }
}