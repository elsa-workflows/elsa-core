using Elsa.Common;
using Elsa.Common.Models;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Workflows;

namespace Elsa.ExternalAuthentication.Services;

/// <summary>
/// Provides a replaceable single-node implementation of atomic external identity linking and just-in-time provisioning.
/// Durable, multi-node hosts should replace this service with a transactional provisioner.
/// </summary>
public sealed class InMemoryExternalIdentityProvisioner(
    IUserStore userStore,
    IUserProvider userProvider,
    IRoleProvider roleProvider,
    IIdentityGenerator identityGenerator,
    ISystemClock clock,
    IExternalAuthenticationHandleHasher handleHasher,
    InMemoryExternalIdentityProvisionerState state) : IExternalIdentityProvisioner, IExternalIdentityLinkManagementStore, IExternalIdentitySignInTracker
{
    private readonly ExternalIdentityUserProvisioningService _userProvisioningService = new(userStore, userProvider, roleProvider, identityGenerator);

    public async ValueTask<ExternalIdentityLink?> FindLinkAsync(string tenantId, string connectionKey, ExternalIdentity identity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = new ExternalIdentityKey(tenantId, ConnectionRevisionCalculator.NormalizeKey(connectionKey), identity.Issuer, handleHasher.Hash(identity.Subject));

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
        var key = new ExternalIdentityKey(request.TenantId, ConnectionRevisionCalculator.NormalizeKey(request.ConnectionKey), request.Identity.Issuer, subjectHash);

        await state.Mutex.WaitAsync(cancellationToken);
        try
        {
            if (state.Links.TryGetValue(key, out var existingLink))
                return new(existingLink.UserId, existingLink, false);

            var (user, wasCreated) = await _userProvisioningService.ResolveAsync(request, state.ReservedUserNames.Add, cancellationToken);
            var link = new ExternalIdentityLink(
                identityGenerator.GenerateId(),
                request.TenantId,
                ConnectionRevisionCalculator.NormalizeKey(request.ConnectionKey),
                request.Identity.Issuer,
                subjectHash,
                null,
                user.Id,
                clock.UtcNow,
                null);
            state.Links[key] = link;
            if (!await _userProvisioningService.ExistsAsync(user, wasCreated, CancellationToken.None))
            {
                state.Links.Remove(key);
                throw new InvalidOperationException("The Elsa user was deleted while its external identity link was being created.");
            }
            return new(user.Id, link, wasCreated, true);
        }
        finally
        {
            state.Mutex.Release();
        }
    }

    public async ValueTask<bool> RecordSuccessfulSignInAsync(
        string tenantId,
        string connectionKey,
        ExternalIdentity identity,
        string userId,
        DateTimeOffset signedInAt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = new ExternalIdentityKey(tenantId, ConnectionRevisionCalculator.NormalizeKey(connectionKey), identity.Issuer, handleHasher.Hash(identity.Subject));

        await state.Mutex.WaitAsync(cancellationToken);
        try
        {
            if (!state.Links.TryGetValue(key, out var link) || !string.Equals(link.UserId, userId, StringComparison.Ordinal))
                return false;

            if (link.LastSignedInAt is null || link.LastSignedInAt < signedInAt)
                state.Links[key] = link with { LastSignedInAt = signedInAt };
            return true;
        }
        finally
        {
            state.Mutex.Release();
        }
    }

    public async ValueTask<ExternalIdentityLinkReplaceResult> ReplaceAsync(ExternalIdentityLinkReplaceRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedConnectionKey = ConnectionRevisionCalculator.NormalizeKey(request.ConnectionKey);
        var replacementKey = new ExternalIdentityKey(request.TenantId, normalizedConnectionKey, request.Identity.Issuer, handleHasher.Hash(request.Identity.Subject));

        await state.Mutex.WaitAsync(cancellationToken);
        try
        {
            var oldEntry = state.Links.FirstOrDefault(x =>
                string.Equals(x.Value.Id, request.LinkId, StringComparison.Ordinal) &&
                string.Equals(x.Value.TenantId, request.TenantId, StringComparison.Ordinal));
            if (oldEntry.Equals(default(KeyValuePair<ExternalIdentityKey, ExternalIdentityLink>)))
                return new ExternalIdentityLinkReplaceResult.NotFound();

            if (state.Links.TryGetValue(replacementKey, out var conflictingLink) &&
                !string.Equals(conflictingLink.Id, oldEntry.Value.Id, StringComparison.Ordinal))
                return new ExternalIdentityLinkReplaceResult.Conflict(oldEntry.Value, conflictingLink);

            var (user, _) = await _userProvisioningService.ResolveAsync(
                new(request.TenantId, normalizedConnectionKey, request.Identity, null, request.UserId),
                cancellationToken: cancellationToken);
            var replacement = new ExternalIdentityLink(
                identityGenerator.GenerateId(),
                request.TenantId,
                normalizedConnectionKey,
                request.Identity.Issuer,
                replacementKey.SubjectHash,
                null,
                user.Id,
                clock.UtcNow,
                null);
            state.Links.Remove(oldEntry.Key);
            state.Links[replacementKey] = replacement;
            if (!await _userProvisioningService.ExistsAsync(user, false, CancellationToken.None))
            {
                state.Links.Remove(replacementKey);
                state.Links[oldEntry.Key] = oldEntry.Value;
                var previousUser = new User { Id = oldEntry.Value.UserId, TenantId = oldEntry.Value.TenantId };
                if (!await _userProvisioningService.ExistsAsync(previousUser, false, CancellationToken.None))
                    state.Links.Remove(oldEntry.Key);
                throw new InvalidOperationException("The Elsa user was deleted while its external identity link was being replaced.");
            }
            return new ExternalIdentityLinkReplaceResult.Success(oldEntry.Value, replacement);
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
                .Where(x => filter.ConnectionKey is null || string.Equals(x.ConnectionKey, ConnectionRevisionCalculator.NormalizeKey(filter.ConnectionKey), StringComparison.Ordinal))
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

}
