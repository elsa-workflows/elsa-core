using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;

namespace Elsa.ExternalAuthentication.Policies;

/// <summary>
/// Safely rejects external identities that have no explicit link.
/// </summary>
public sealed class RejectUnlinkedIdentityPolicy : IUnlinkedIdentityPolicy
{
    public const string PolicyType = "reject";
    public const string SafeReason = "identity_unlinked";

    public string Type => PolicyType;

    public UnlinkedIdentityPolicyDescriptor Describe() => new(
        Type,
        "Reject unlinked identities",
        "Requires an administrator to link an external identity before it can sign in.",
        1,
        [],
        null);

    public ValueTask<UnlinkedIdentityDecision> EvaluateAsync(UnlinkedIdentityContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<UnlinkedIdentityDecision>(new UnlinkedIdentityDecision.Reject(SafeReason));
    }
}
