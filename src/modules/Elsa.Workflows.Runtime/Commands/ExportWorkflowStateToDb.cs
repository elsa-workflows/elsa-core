using Elsa.Mediator.Services;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Runtime.Commands;

public record ExportWorkflowStateToDb(WorkflowState WorkflowState) : ICommand;