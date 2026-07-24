using Elsa.ExternalAuthentication.Models;

namespace Elsa.ExternalAuthentication.Services;

/// <summary>
/// Shared single-node state for the scoped in-memory identity provisioner.
/// </summary>
public sealed class InMemoryExternalIdentityProvisionerState
{
    internal SemaphoreSlim Mutex { get; } = new(1, 1);
    internal IDictionary<ExternalIdentityKey, ExternalIdentityLink> Links { get; } = new Dictionary<ExternalIdentityKey, ExternalIdentityLink>();
    internal ISet<string> ReservedUserNames { get; } = new HashSet<string>(StringComparer.Ordinal);
}

internal sealed record ExternalIdentityKey(string TenantId, string ConnectionId, string Issuer, string SubjectHash);
