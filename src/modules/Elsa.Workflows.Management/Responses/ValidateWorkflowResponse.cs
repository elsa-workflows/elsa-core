using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Requests;

namespace Elsa.Workflows.Management.Responses;

/// <summary>
/// Provides a response to a <see cref="ValidateWorkflowRequest"/>.
/// </summary>
/// <param name="ValidationErrors">The validation errors, if any.</param>
public record ValidateWorkflowResponse(ICollection<WorkflowValidationError> ValidationErrors);