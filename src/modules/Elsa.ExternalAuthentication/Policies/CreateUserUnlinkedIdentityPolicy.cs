using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;

namespace Elsa.ExternalAuthentication.Policies;

/// <summary>
/// Requests just-in-time creation of a credential-less Elsa user for an unlinked external identity.
/// </summary>
public sealed class CreateUserUnlinkedIdentityPolicy : IUnlinkedIdentityPolicy
{
    public const string PolicyType = "create-user";
    private const string DefaultUserNamePrefix = "external";

    public string Type => PolicyType;

    public UnlinkedIdentityPolicyDescriptor Describe() => new(
        Type,
        "Create a user",
        "Creates a credential-less Elsa user and an external identity link after successful external authentication.",
        1,
        [],
        null);

    public ValueTask<UnlinkedIdentityDecision> EvaluateAsync(UnlinkedIdentityContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<UnlinkedIdentityDecision>(new UnlinkedIdentityDecision.CreateUser(new UserCreationProposal(DefaultUserNamePrefix)));
    }
}
