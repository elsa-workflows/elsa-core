using Elsa.Common;
using Elsa.Common.Models;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Workflows;

namespace Elsa.ExternalAuthentication.Services;

/// <summary>
/// Provides a replaceable single-node implementation of atomic external identity linking and just-in-time provisioning.
/// Durable, multi-node hosts should replace this service with a transactional provisioner.
/// </summary>
public sealed class InMemoryExternalIdentityProvisioner(
    IUserStore userStore,
    IUserProvider userProvider,
    IIdentityGenerator identityGenerator,
    ISystemClock clock,
    IExternalAuthenticationHandleHasher handleHasher,
    InMemoryExternalIdentityProvisionerState state) : IExternalIdentityProvisioner, IExternalIdentityLinkManagementStore
{
    private const int MaximumUserNameAttempts = 10;

    public async ValueTask<ExternalIdentityLink?> FindLinkAsync(string tenantId, string connectionId, ExternalIdentity identity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = new ExternalIdentityKey(tenantId, connectionId, identity.Issuer, handleHasher.Hash(identity.Subject));

        await state.Mutex.WaitAsync(cancellationToken);
        try
        {
            return state.Links.TryGetValue(key, out var link) ? link : null;
        }
        finally
        {
            state.Mutex.Release();
        }
    }

    public async ValueTask<ProvisioningResult> CreateLinkOrGetExistingAsync(ProvisioningRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        var subjectHash = handleHasher.Hash(request.Identity.Subject);
        var key = new ExternalIdentityKey(request.TenantId, request.ConnectionId, request.Identity.Issuer, subjectHash);

        await state.Mutex.WaitAsync(cancellationToken);
        try
        {
            if (state.Links.TryGetValue(key, out var existingLink))
                return new ProvisioningResult(existingLink.UserId, existingLink, false);

            var (user, wasCreated) = await ResolveUserAsync(request, cancellationToken);
            var link = new ExternalIdentityLink(
                identityGenerator.GenerateId(),
                request.TenantId,
                request.ConnectionId,
                request.Identity.Issuer,
                subjectHash,
                null,
                user.Id,
                clock.UtcNow,
                null);
            state.Links[key] = link;
            return new ProvisioningResult(user.Id, link, wasCreated, true);
        }
        finally
        {
            state.Mutex.Release();
        }
    }

    public async ValueTask<Page<ExternalIdentityLink>> FindAsync(ExternalIdentityLinkFilter filter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        cancellationToken.ThrowIfCancellationRequested();

        await state.Mutex.WaitAsync(cancellationToken);
        try
        {
            var links = state.Links.Values
                .Where(x => string.Equals(x.TenantId, filter.TenantId, StringComparison.Ordinal))
                .Where(x => filter.UserId is null || string.Equals(x.UserId, filter.UserId, StringComparison.Ordinal))
                .Where(x => filter.ConnectionId is null || string.Equals(x.ConnectionId, filter.ConnectionId, StringComparison.Ordinal))
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Id, StringComparer.Ordinal)
                .ToArray();
            return Page.Of<ExternalIdentityLink>(links, links.Length);
        }
        finally
        {
            state.Mutex.Release();
        }
    }

    public async ValueTask<bool> DeleteAsync(string tenantId, string linkId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await state.Mutex.WaitAsync(cancellationToken);
        try
        {
            var entry = state.Links.FirstOrDefault(x => string.Equals(x.Value.Id, linkId, StringComparison.Ordinal) && string.Equals(x.Value.TenantId, tenantId, StringComparison.Ordinal));
            return !entry.Equals(default(KeyValuePair<ExternalIdentityKey, ExternalIdentityLink>)) && state.Links.Remove(entry.Key);
        }
        finally
        {
            state.Mutex.Release();
        }
    }

    private async ValueTask<(User User, bool WasCreated)> ResolveUserAsync(ProvisioningRequest request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.ExistingUserId))
        {
            var user = await userProvider.FindAsync(new UserFilter { Id = request.ExistingUserId }, cancellationToken)
                ?? throw new InvalidOperationException("The requested Elsa user does not exist.");
            if (!string.Equals(user.TenantId, request.TenantId, StringComparison.Ordinal))
                throw new InvalidOperationException("The requested Elsa user is outside the target tenant.");

            return (user, false);
        }

        var proposal = request.Proposal ?? throw new InvalidOperationException("A user creation proposal is required for an unlinked external identity.");
        var userNamePrefix = NormalizeUserNamePrefix(proposal.UserNamePrefix);
        for (var attempt = 0; attempt < MaximumUserNameAttempts; attempt++)
        {
            var userName = $"{userNamePrefix}-{identityGenerator.GenerateId()}";
            if (!state.ReservedUserNames.Add(userName) || await userProvider.FindAsync(new UserFilter { Name = userName }, cancellationToken) is not null)
                continue;

            var user = new User
            {
                Id = identityGenerator.GenerateId(),
                Name = userName,
                TenantId = request.TenantId,
                HashedPassword = null,
                HashedPasswordSalt = null
            };
            await userStore.SaveAsync(user, cancellationToken);
            return (user, true);
        }

        throw new InvalidOperationException("A unique Elsa user name could not be reserved for the external identity.");
    }

    private static string NormalizeUserNamePrefix(string prefix)
    {
        var normalized = new string((prefix ?? string.Empty).Trim().Where(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_').ToArray());
        return string.IsNullOrEmpty(normalized) ? "external" : normalized;
    }

}
