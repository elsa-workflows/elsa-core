using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Http.Features;
using Elsa.SasTokens.Features;
using Elsa.Workflows.Api.Constants;
using Elsa.Workflows.Api.Options;
using Elsa.Workflows.Api.Requirements;
using Elsa.Workflows.Api.Serialization;
using Elsa.Workflows.Api.Services;
using Elsa.Workflows.Management.Features;
using Elsa.Workflows.Runtime.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Api.Features;

/// <summary>
/// Adds workflows API features.
/// </summary>
[DependsOn(typeof(WorkflowInstancesFeature))]
[DependsOn(typeof(WorkflowManagementFeature))]
[DependsOn(typeof(WorkflowRuntimeFeature))]
[DependsOn(typeof(HttpFeature))]
[DependsOn(typeof(SasTokensFeature))]
public class WorkflowsApiFeature : FeatureBase
{
    private bool IsReadOnlyMode { get; set; }

    /// <inheritdoc />
    public WorkflowsApiFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// Enables or disables read-only mode for resources such as workflow definitions.
    /// </summary>
    /// <returns></returns>
    public WorkflowsApiFeature UseReadOnlyMode(bool enabled)
    {
        IsReadOnlyMode = enabled;
        return this;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.AddFastEndpointsAssembly(GetType());
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddSerializationOptionsConfigurator<SerializationConfigurator>();
        Module.AddFastEndpointsFromModule();

        Services.Configure<ApiOptions>(options =>
        {
            options.IsReadOnlyMode = IsReadOnlyMode;
        });

        Services.AddSingleton<IWorkflowDefinitionLinkService, WorkflowDefinitionLinkService>();

        Services.AddScoped<IAuthorizationHandler, NotReadOnlyRequirementHandler>();
        Services.Configure<AuthorizationOptions>(options =>
        {
            options.AddPolicy(AuthorizationPolicies.NotReadOnlyPolicy, policy => policy.AddRequirements(new NotReadOnlyRequirement()));
        });
    }
}