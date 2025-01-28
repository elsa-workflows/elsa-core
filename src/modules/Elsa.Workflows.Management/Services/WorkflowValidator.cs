using Elsa.Mediator.Contracts;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Notifications;

namespace Elsa.Workflows.Management.Services;

/// <inheritdoc />
public class WorkflowValidator(INotificationSender notificationSender) : IWorkflowValidator
{
    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowValidationError>> ValidateAsync(Workflow workflow, CancellationToken cancellationToken = default)
    {
        var validationErrors = new List<WorkflowValidationError>();
        var notification = new WorkflowDefinitionValidating(workflow, validationErrors);
        await notificationSender.SendAsync(notification, cancellationToken);
        return validationErrors;
    }
}