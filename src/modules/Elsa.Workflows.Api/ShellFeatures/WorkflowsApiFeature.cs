using CShells.FastEndpoints.Features;
using CShells.Features;
using Elsa.Extensions;
using Elsa.SasTokens.ShellFeatures;
using Elsa.ShellFeatures;
using Elsa.Workflows.Api.Constants;
using Elsa.Workflows.Api.Requirements;
using Elsa.Workflows.Api.Security;
using Elsa.Workflows.Api.Serialization;
using Elsa.Workflows.Api.Services;
using Elsa.Workflows.Management.ShellFeatures;
using Elsa.Workflows.Runtime.ShellFeatures;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Api.ShellFeatures;

/// <summary>
/// Adds workflows API features with FastEndpoints support.
/// </summary>
/// <remarks>
/// This feature implements <see cref="IFastEndpointsShellFeature"/> to indicate that this assembly
/// contains FastEndpoints that should be automatically discovered and registered.
/// </remarks>
[ManifestFeatureCategory("Workflows")]
[ManifestFeatureCategory("API")]
[ShellFeature(
    DisplayName = "Workflows API",
    Description = "Provides REST API endpoints for workflow management",
    DependsOn =
[
    typeof(ElsaFastEndpointsFeature),
    typeof(WorkflowInstancesFeature),
    typeof(WorkflowManagementFeature),
    typeof(WorkflowRuntimeFeature),
    typeof(SasTokensFeature)
])]
[UsedImplicitly]
public class WorkflowsApiFeature : IFastEndpointsShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSerializationOptionsConfigurator<SerializationConfigurator>();
        services.AddScoped<IWorkflowDefinitionLinker, StaticWorkflowDefinitionLinker>();
        services.AddScoped<WorkflowDefinitionScriptAuthorizationService>();
        services.AddScoped<IAuthorizationHandler, NotReadOnlyRequirementHandler>();
        services.Configure<AuthorizationOptions>(options =>
        {
            options.AddPolicy(AuthorizationPolicies.NotReadOnlyPolicy, policy => policy.AddRequirements(new NotReadOnlyRequirement()));
        });
        services.AddScoped<IWorkflowInstanceExportNameProvider, DefaultWorkflowInstanceExportNameProvider>();
    }
}
