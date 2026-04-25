using CShells.FastEndpoints.Features;
using CShells.Features;
using Elsa.ShellFeatures;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.Admin.ShellFeatures;

/// <summary>
/// Activates the administrative HTTP endpoints (<c>/admin/workflow-runtime/pause</c>, <c>/resume</c>, <c>/status</c>, <c>/force</c>)
/// that wrap the workflow-runtime graceful-shutdown machinery. Implements <see cref="IFastEndpointsShellFeature"/>
/// so the FastEndpoints discovery loads endpoints from this assembly.
/// </summary>
[ShellFeature(
    DisplayName = "Workflow Runtime Admin",
    Description = "Provides administrative pause/resume/status/force endpoints for the workflow runtime",
    DependsOn = [typeof(ElsaFastEndpointsFeature)])]
[UsedImplicitly]
public sealed class WorkflowRuntimeAdminFeature : IFastEndpointsShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        // No-op: marker class for assembly-level endpoint discovery.
    }
}
