namespace Elsa.Diagnostics.ConsoleLogs.Contracts;

/// <summary>
/// Provides ambient metadata for console log capture.
/// </summary>
public interface IConsoleLogContextAccessor
{
    /// <summary>
    /// Gets the metadata attached to the current async execution context.
    /// </summary>
    IReadOnlyDictionary<string, string> GetMetadata();

    /// <summary>
    /// Pushes a metadata value for the current async execution context.
    /// </summary>
    IDisposable PushMetadata(string key, string value);

    /// <summary>
    /// Pushes the current workflow instance ID for console log ownership.
    /// </summary>
    IDisposable PushWorkflowInstanceId(string workflowInstanceId);
}
