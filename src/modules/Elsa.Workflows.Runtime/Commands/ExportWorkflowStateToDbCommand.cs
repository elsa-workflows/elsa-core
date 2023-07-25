using Elsa.Mediator.Contracts;
using Elsa.Mediator.Models;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Runtime.Commands;

/// <summary>
/// A command that exports a workflow state to a database.
/// </summary>
/// <param name="WorkflowState">The workflow state to export.</param>
public record ExportWorkflowStateToDbCommand(WorkflowState WorkflowState) : ICommand<Unit>;