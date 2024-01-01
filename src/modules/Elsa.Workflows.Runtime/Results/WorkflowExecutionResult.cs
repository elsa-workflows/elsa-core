using Elsa.Workflows.Models;

namespace Elsa.Workflows.Runtime.Results;

public record WorkflowExecutionResult(string WorkflowInstanceId, WorkflowStatus Status, WorkflowSubStatus SubStatus, ICollection<Bookmark> Bookmarks, ICollection<ActivityIncident> Incidents, string? TriggeredActivityId = null);