using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Sinks.Contracts;

namespace Elsa.Workflows.Sinks.Models;

/// <summary>
/// Provides context to the <see cref="Elsa.Workflows.Sinks.Contracts.IWorkflowSink"/> implementations.
/// </summary>
public record WorkflowSinkContext(WorkflowState WorkflowState, Workflow Workflow, CancellationToken CancellationToken);