using Elsa.Workflows.Models;

namespace Elsa.Workflows;

/// <summary>
/// Resolves, validates and invokes the converter selected by an output binding.
/// </summary>
public interface IOutputConverterInvoker
{
    /// <summary>
    /// Resolves and invokes the configured converter after validating its settings and declared types.
    /// </summary>
    object? Invoke(
        ActivityExecutionContext activityExecutionContext,
        Output output,
        string outputName,
        Type sourceType,
        object value,
        OutputBindingDestination destination);
}
