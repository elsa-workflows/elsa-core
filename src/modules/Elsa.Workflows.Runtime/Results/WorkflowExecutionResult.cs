using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Runtime.Contracts;

public record WorkflowExecutionResult(string WorkflowInstanceId, WorkflowStatus Status, WorkflowSubStatus SubStatus, ICollection<Bookmark> Bookmarks, string? TriggeredActivityId = null, WorkflowFaultState? Fault = default);