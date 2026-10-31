using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.PlatformIntegration.Contracts;
using Microsoft.Extensions.Options;

namespace Elsa.Studio.PlatformIntegration.Services;

internal sealed class PlatformWorkflowSubmissionService(
    IOptions<PlatformSubmitOptions> options,
    IPlatformWorkflowSnapshotPackager packager,
    IPlatformArtifactSubmitClient submitClient) : IPlatformWorkflowSubmissionService
{
    public async Task<PlatformSubmitResult> SubmitAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default)
    {
        var value = options.Value;
        var package = packager.Package(workflowDefinition, value);
        return await submitClient.SubmitAsync(package, value, cancellationToken);
    }
}
