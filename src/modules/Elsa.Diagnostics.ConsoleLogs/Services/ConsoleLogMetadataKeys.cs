namespace Elsa.Diagnostics.ConsoleLogs.Services;

/// <summary>
/// Well-known metadata keys for console log enrichment.
/// </summary>
public static class ConsoleLogMetadataKeys
{
    /// <summary>
    /// Identifies the workflow instance that owned a console write.
    /// </summary>
    public const string WorkflowInstanceId = "elsa.workflowInstanceId";
}
