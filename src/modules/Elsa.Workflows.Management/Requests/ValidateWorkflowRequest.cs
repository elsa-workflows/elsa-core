using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Management.Requests;

public record ValidateWorkflowRequest(WorkflowDefinition WorkflowDefinition) : IRequest<ValidateWorkflowResponse>;
public record ValidateWorkflowResponse(IEnumerable<string> ValidationErrors);