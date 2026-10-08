using Elsa.Common.Multitenancy;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Persistence.EFCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Workflows.Admission.Persistence.EFCore.Features;

/// <summary>Registers the admission context. A supported provider and explicit trusted scope are required.</summary>
public sealed class EFCoreAdmissionPersistenceFeature(IModule module)
    : PersistenceFeatureBase<EFCoreAdmissionPersistenceFeature, AdmissionElsaDbContext>(module)
{
    public string TenantId { get; set; } = null!;
    public string EnvironmentId { get; set; } = null!;

    public override void Apply()
    {
        var scope = new AdmissionPersistenceScope(TenantId, EnvironmentId);
        scope.Validate();
        // The shared factory requires an accessor even in a single-scope, non-multitenant host.
        // Preserve an accessor supplied by the host, as the existing Connections feature does.
        Services.TryAddSingleton<ITenantAccessor, DefaultTenantAccessor>();
        base.Apply();
        Services.AddSingleton(scope);
        Services.AddScoped<IAdmissionStore, EFCoreAdmissionStore>();
    }
}
