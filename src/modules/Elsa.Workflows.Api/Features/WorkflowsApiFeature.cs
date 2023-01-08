using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Api.Features;

/// <summary>
/// Adds workflows API features.
/// </summary>
public class WorkflowsApiFeature : FeatureBase
{
    /// <inheritdoc />
    public WorkflowsApiFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// A delegate that configures the policy requirements for the /tasks/{taskId}/complete API endpoint. 
    /// </summary>
    public Action<AuthorizationPolicyBuilder> CompleteTaskPolicy { get; set; } = policy => policy.RequireAuthenticatedUser();

    /// <inheritdoc />
    public override void Configure()
    {
        Module.AddFastEndpointsAssembly(GetType());
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddAuthorization(auth => auth.AddPolicy("CompleteTask", CompleteTaskPolicy));
        Module.AddFastEndpointsFromModule();
    }
}