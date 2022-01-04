using Elsa.Mediator.Contracts;
using Elsa.Persistence.Entities;

namespace Elsa.Persistence.Requests;

public record FindWorkflowInstance(string InstanceId) : IRequest<WorkflowInstance?>;