using Elsa.Mediator.Services;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Sinks.Commands;

internal record ProcessWorkflowState(WorkflowState WorkflowState) : ICommand;