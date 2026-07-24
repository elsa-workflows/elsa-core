using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Options;
using Microsoft.Extensions.Options;

namespace Elsa.ExternalAuthentication.Services;

/// <summary>
/// Resolves normalized external identities to Elsa users and delegates tuple uniqueness to the atomic provisioner.
/// </summary>
public sealed class DefaultExternalIdentityResolver(
    IExternalIdentityProvisioner provisioner,
    IEnumerable<IUnlinkedIdentityPolicy> policies,
    IOptions<ExternalAuthenticationOptions> options) : IExternalIdentityResolver
{
    private readonly IReadOnlyDictionary<string, IUnlinkedIdentityPolicy> _policies = policies.ToDictionary(x => x.Type, StringComparer.Ordinal);

    public async ValueTask<ExternalIdentityResolution> ResolveAsync(ExternalIdentityResolutionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var existingLink = await provisioner.FindLinkAsync(context.TargetTenantId, context.Connection.Connection.Id, context.Identity, cancellationToken);
        if (existingLink is not null)
        {
            ValidateLink(existingLink, context);
            return new ExternalIdentityResolution(existingLink.UserId, false);
        }

        var selection = GetPolicySelection(context.Connection);
        var policy = _policies.GetValueOrDefault(selection.Type)
            ?? throw new InvalidOperationException($"The unlinked identity policy '{selection.Type}' is not available.");
        var decision = await policy.EvaluateAsync(new UnlinkedIdentityContext(
            context.TargetTenantId,
            context.Connection,
            context.Identity,
            context.ProjectedClaims,
            selection.Settings), cancellationToken);

        var result = decision switch
        {
            UnlinkedIdentityDecision.Reject reject => throw new ExternalIdentityUnlinkedException(reject.SafeReason),
            UnlinkedIdentityDecision.CreateUser createUser => await provisioner.CreateLinkOrGetExistingAsync(
                new ProvisioningRequest(context.TargetTenantId, context.Connection.Connection.Id, context.Identity, createUser.Proposal), cancellationToken),
            UnlinkedIdentityDecision.LinkExistingUser linkExistingUser => await provisioner.CreateLinkOrGetExistingAsync(
                new ProvisioningRequest(context.TargetTenantId, context.Connection.Connection.Id, context.Identity, null, linkExistingUser.UserId), cancellationToken),
            _ => throw new InvalidOperationException("The unlinked identity policy returned an unsupported decision.")
        };

        ValidateLink(result.Link, context);
        return new ExternalIdentityResolution(result.UserId, result.WasCreated);
    }

    private PolicySelection GetPolicySelection(EffectiveIdentityProviderConnection connection)
    {
        var configuredPolicy = connection.Connection.UnlinkedPolicy;
        var mayOverride = connection.Ownership == ConnectionSourceOwnership.Configuration || options.Value.UnlinkedIdentityPolicy.AllowDatabaseConnectionOverride;
        if (configuredPolicy is not null && mayOverride)
            return configuredPolicy;

        return new PolicySelection(options.Value.UnlinkedIdentityPolicy.DefaultType, 1, default);
    }

    private static void ValidateLink(ExternalIdentityLink link, ExternalIdentityResolutionContext context)
    {
        if (!string.Equals(link.TenantId, context.TargetTenantId, StringComparison.Ordinal) ||
            !string.Equals(link.ConnectionId, context.Connection.Connection.Id, StringComparison.Ordinal) ||
            !string.Equals(link.Issuer, context.Identity.Issuer, StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(link.UserId))
        {
            throw new InvalidOperationException("The external identity provisioner returned a link outside the requested identity scope.");
        }
    }
}

/// <summary>
/// Signals that a successfully authenticated external identity has no permitted Elsa user link.
/// Endpoint code must translate this exception to the safe <c>identity_unlinked</c> broker category.
/// </summary>
public sealed class ExternalIdentityUnlinkedException(string safeReason) : InvalidOperationException
{
    public string SafeReason { get; } = safeReason;
}
