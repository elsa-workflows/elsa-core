using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Management.Commands;

public record SaveWorkflowDefinitionCommand(WorkflowDefinition WorkflowDefinition) : ICommand;