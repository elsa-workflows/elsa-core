using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Studio.Workflows.UI.Models;

namespace Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Models;

/// <summary>
/// Represents a call stack entry for an activity execution.
/// </summary>
public record ActivityCallStackEntry(
    ActivityExecutionRecord Record,
    ActivityDescriptor? ActivityDescriptor,
    ActivityDisplaySettings? ActivityDisplaySettings,
    TimeSpan? Duration
);
