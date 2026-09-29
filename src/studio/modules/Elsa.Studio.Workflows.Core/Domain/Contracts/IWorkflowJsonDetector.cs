namespace Elsa.Studio.Workflows.Domain.Contracts;

/// <summary>
/// A service that detects whether a JSON string is a workflow definition.
/// </summary>
public interface IWorkflowJsonDetector
{
    /// <summary>
    /// Checks whether the provided JSON is a workflow schema.
    /// </summary>
    bool IsWorkflowSchema(string json);
}