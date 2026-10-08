using Elsa.Persistence.EFCore;
using Elsa.Workflows.Admission.Persistence.EFCore.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Admission.Persistence.EFCore.ShellFeatures;

/// <summary>
/// Registers the admission context and trusted scope for Shell hosts.
/// Select a concrete provider feature; this base is not a standalone shell feature.
/// </summary>
public abstract class EFCoreAdmissionPersistenceShellFeatureBase : PersistenceShellFeatureBase<AdmissionElsaDbContext>
{
    public string TenantId { get; set; } = null!;
    public string EnvironmentId { get; set; } = null!;

    protected override void OnConfiguring(IServiceCollection services)
    {
        var scope = new AdmissionPersistenceScope(TenantId, EnvironmentId);
        scope.Validate();
        services.AddAdmissionPersistence(scope);
    }
}
