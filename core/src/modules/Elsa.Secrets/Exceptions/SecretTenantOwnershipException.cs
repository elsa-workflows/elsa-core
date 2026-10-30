namespace Elsa.Secrets.Exceptions;

/// <summary>
/// Thrown when the current tenant context writes a secret it does not own. This includes a tenant-agnostic (<c>*</c>)
/// secret: every tenant can read it, but only the <c>*</c> context can change it.
/// </summary>
public class SecretTenantOwnershipException(string secretName) : InvalidOperationException($"A secret named '{secretName}' belongs to another tenant.");
