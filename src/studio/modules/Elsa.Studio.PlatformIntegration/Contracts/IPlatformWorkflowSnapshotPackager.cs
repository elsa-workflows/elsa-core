using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

namespace Elsa.Studio.PlatformIntegration.Contracts;

/// <summary>
/// Packages Studio workflow definitions as Platform artifact envelopes.
/// </summary>
public interface IPlatformWorkflowSnapshotPackager
{
    /// <summary>
    /// Packages the specified workflow definition.
    /// </summary>
    PlatformWorkflowSubmitPackage Package(WorkflowDefinition workflowDefinition, PlatformSubmitOptions options, DateTimeOffset? packagedAt = null);
}
