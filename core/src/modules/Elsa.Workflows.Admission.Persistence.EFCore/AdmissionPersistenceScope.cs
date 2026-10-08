namespace Elsa.Workflows.Admission.Persistence.EFCore;

/// <summary>Deployment-controlled tenant/environment. Never derived from an event or request.</summary>
public sealed record AdmissionPersistenceScope(string TenantId, string EnvironmentId)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(TenantId) || TenantId.Length > 256 ||
            string.IsNullOrWhiteSpace(EnvironmentId) || EnvironmentId.Length > 256)
        {
            throw new ArgumentException("Admission persistence requires an explicit bounded tenant and environment.");
        }
    }
}
