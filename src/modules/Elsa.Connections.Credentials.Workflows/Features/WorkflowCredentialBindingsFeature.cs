using Elsa.Connections.Contracts;
using Elsa.Connections.Credentials.Workflows.Contracts;
using Elsa.Connections.Credentials.Workflows.Services;
using Elsa.Connections.Features;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Connections.Credentials.Workflows.Features;

[DependsOn(typeof(ConnectionsFeature))]
public sealed class WorkflowCredentialBindingsFeature(IModule module) : FeatureBase(module)
{
    /// <summary>Configured by the host; workflow data cannot select the target environment.</summary>
    public string? EnvironmentId { get; set; }

    public override void Apply()
    {
        Services.Configure<WorkflowCredentialBindingOptions>(options => options.EnvironmentId = EnvironmentId);
        Services.TryAddScoped<IConnectionCredentialBindingStore, UnavailableConnectionCredentialBindingStore>();
        Services.TryAddScoped<IConnectionCredentialBindingUseAuthorizer, DenyAllConnectionCredentialBindingUseAuthorizer>();
        Services.TryAddScoped<IConnectionCredentialBindingManagementAuthorizer, DenyAllConnectionCredentialBindingManagementAuthorizer>();
        Services.TryAddScoped<IWorkflowCredentialResolver, WorkflowCredentialResolver>();
        Services.TryAddScoped<IWorkflowCredentialBindingManager, WorkflowCredentialBindingManager>();
    }
}
