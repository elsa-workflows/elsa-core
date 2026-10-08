using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.Contracts;

/// <summary>
/// Validator that validate a given trigger payload.
/// </summary>
public interface ITriggerPayloadValidator<TPayload>
{
    /// <summary>
    /// Validate a trigger payload. If trigger is not valid, error will be add in this list.
    /// </summary>
    Task ValidateAsync(
        TPayload payload,
        Workflow workflow,
        StoredTrigger trigger,
        ICollection<WorkflowValidationError> validationErrors,
        CancellationToken cancellationToken);
}