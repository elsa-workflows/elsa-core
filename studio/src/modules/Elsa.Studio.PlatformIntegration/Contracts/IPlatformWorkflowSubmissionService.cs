using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

namespace Elsa.Studio.PlatformIntegration.Contracts;

/// <summary>
/// Coordinates workflow submission to Elsa Platform.
/// </summary>
public interface IPlatformWorkflowSubmissionService
{
    /// <summary>
    /// Submits the specified published workflow definition.
    /// </summary>
    Task<PlatformSubmitResult> SubmitAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default);
}
