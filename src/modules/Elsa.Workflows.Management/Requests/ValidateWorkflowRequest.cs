using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Management.Responses;

namespace Elsa.Workflows.Management.Requests;

/// <summary>
/// A request to validate a workflow definition.
/// </summary>
/// <param name="Workflow">The workflow materialized from the definition.</param>
public record ValidateWorkflowRequest(Workflow Workflow) : IRequest<ValidateWorkflowResponse>;