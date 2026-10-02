using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

namespace Elsa.Studio.Workflows.Domain.Models;

/// <summary>
/// Represents the result of workflow import.
/// </summary>
public class WorkflowImportResult
{
    /// <summary>
    /// Gets or sets the file name.
    /// </summary>
    public string FileName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the workflow definition.
    /// </summary>
    public WorkflowDefinition? WorkflowDefinition { get; set; }
    /// <summary>
    /// Gets or sets the failure.
    /// </summary>
    public WorkflowImportFailure? Failure { get; set; }
    /// <summary>
    /// Provides the is success.
    /// </summary>
    public bool IsSuccess => Failure == null;
}
