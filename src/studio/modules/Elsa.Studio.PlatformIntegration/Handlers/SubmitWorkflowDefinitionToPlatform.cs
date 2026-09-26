using Elsa.Studio.Contracts;
using Elsa.Studio.PlatformIntegration.Contracts;
using Elsa.Studio.Workflows.Domain.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Studio.PlatformIntegration.Handlers;

/// <summary>
/// Submits published workflow definitions to Elsa Platform when the module is configured.
/// </summary>
public sealed class SubmitWorkflowDefinitionToPlatform(
    IOptions<PlatformSubmitOptions> options,
    IPlatformWorkflowSubmissionService submissionService,
    ILogger<SubmitWorkflowDefinitionToPlatform> logger) : INotificationHandler<WorkflowDefinitionPublished>
{
    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionPublished notification, CancellationToken cancellationToken)
    {
        var value = options.Value;

        if (!value.Enabled || !value.SubmitOnWorkflowPublished || !value.IsConfigured)
            return;

        try
        {
            var result = await submissionService.SubmitAsync(notification.WorkflowDefinition, cancellationToken);

            if (result.Succeeded)
            {
                logger.LogInformation(
                    "Submitted workflow definition {WorkflowDefinitionId} to Elsa Platform as artifact {ArtifactId}.",
                    notification.WorkflowDefinition.DefinitionId,
                    result.ArtifactId);
                return;
            }

            logger.LogWarning(
                "Elsa Platform workflow submission completed with status {Status}: {Message}",
                result.Status,
                result.Message);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Elsa Platform workflow submission failed after Studio published workflow definition {WorkflowDefinitionId}.",
                notification.WorkflowDefinition.DefinitionId);
        }
    }
}
