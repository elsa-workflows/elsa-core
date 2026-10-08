using System.Text.Json;

namespace Elsa.Workflows.Api.Models;

/// <summary>
/// Represents a workflow state that can be exported and imported.
/// </summary>
public record ExportedWorkflowState(
    JsonElement WorkflowState, 
    JsonElement? Bookmarks,
    JsonElement? ActivityExecutionRecords, 
    JsonElement? WorkflowExecutionLogRecords);