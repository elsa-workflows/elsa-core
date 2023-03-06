using Elsa.Mediator.Contracts;
using Elsa.Mediator.Models;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Runtime.Commands;

public record ExportWorkflowStateToDbCommand(WorkflowState WorkflowState) : ICommand<Unit>;