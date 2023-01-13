using Elsa.Mediator.Models;
using Elsa.Mediator.Services;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Runtime.Commands;

public record ExportWorkflowStateToDbCommand(WorkflowState WorkflowState) : ICommand<Unit>;