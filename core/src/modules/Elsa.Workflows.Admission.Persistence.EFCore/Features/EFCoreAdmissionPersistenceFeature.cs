using Elsa.Features.Services;
using Elsa.Persistence.EFCore;
using Elsa.Workflows.Admission.Persistence.EFCore.Extensions;

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
        base.Apply();
        Services.AddAdmissionPersistence(scope);
    }
}
