using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Studio.Workflows.UI.Models;

namespace Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Models;

/// <summary>
/// Represents the journal entry record.
/// </summary>
public record JournalEntry(
    WorkflowExecutionLogRecord Record,
    ActivityDescriptor? ActivityDescriptor,
    ActivityDisplaySettings? ActivityDisplaySettings,
    bool IsEven,
    TimeSpan TimeMetric
);