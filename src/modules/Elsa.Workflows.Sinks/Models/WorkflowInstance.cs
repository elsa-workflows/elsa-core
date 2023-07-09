using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Sinks.Models;

/// <summary>
/// Provides context to the <see cref="Elsa.Workflows.Sinks.Contracts.IWorkflowSink"/> implementations.
/// </summary>
public record WorkflowSinkContext(WorkflowState WorkflowState, Workflow Workflow, CancellationToken CancellationToken);