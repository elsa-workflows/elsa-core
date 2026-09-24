using Elsa.Connections.Contracts;
using Elsa.Connections.Credentials.Workflows.Contracts;
using Elsa.Connections.Credentials.Workflows.Services;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Connections.Credentials.Workflows.Features;

/// <summary>Opt-in durable workflow-use grants; without this feature the binding adapter remains deny-all.</summary>
[DependsOn(typeof(WorkflowCredentialBindingsFeature))]
public sealed class WorkflowCredentialUseGrantsFeature(IModule module) : FeatureBase(module)
{
    public override void Apply()
    {
        Services.TryAddScoped<IConnectionCredentialUseGrantStore, UnavailableConnectionCredentialUseGrantStore>();
        Services.TryAddScoped<IConnectionCredentialGrantManagementAuthorizer, DenyAllConnectionCredentialGrantManagementAuthorizer>();
        Services.AddScoped<IWorkflowCredentialGrantManager, WorkflowCredentialGrantManager>();
        Services.AddScoped<IConnectionCredentialBindingUseAuthorizer>(sp => new StoredConnectionCredentialBindingUseAuthorizer(
            sp.GetRequiredService<IConnectionCredentialUseGrantStore>(), sp.GetService<IConnectionLifecycleStore>()));
    }
}
