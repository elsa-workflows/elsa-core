using Elsa.Common;
using Elsa.Common.Models;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Services;
using Elsa.Extensions;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Elsa.ExternalAuthentication.Persistence.EFCore.Stores;

/// <summary>
/// Creates users and external links, converging safely on the durable unique link.
/// </summary>
/// <remarks>
/// Users are resolved through <see cref="IUserProvider"/> and written through <see cref="IUserStore"/>, so the identity
/// aggregate stays behind its own contracts and this provisioner works with any user directory. That means user and link
/// creation are no longer covered by a single database transaction; the unique <c>IX_ExternalIdentityLink_Identity</c>
/// index remains the sole arbiter of the "at most one link per external identity" invariant.
/// </remarks>
public sealed class EFCoreExternalIdentityProvisioner(
    IDbContextFactory<ExternalAuthenticationElsaDbContext> dbContextFactory,
    IUserStore userStore,
    IUserProvider userProvider,
    IRoleProvider roleProvider,
    IExternalAuthenticationHandleHasher handleHasher,
    IIdentityGenerator identityGenerator,
    ISystemClock clock,
    ILogger<EFCoreExternalIdentityProvisioner> logger) : IExternalIdentityProvisioner, IExternalIdentityLinkManagementStore
{
    private const int MaximumUserNameAttempts = 10;

    public async ValueTask<ExternalIdentityLink?> FindLinkAsync(string tenantId, string connectionKey, ExternalIdentity identity, CancellationToken cancellationToken = default)
    {
        var subjectHash = handleHasher.Hash(identity.Subject);
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var normalizedKey = ConnectionRevisionCalculator.NormalizeKey(connectionKey);
        var link = await dbContext.ExternalIdentityLinks.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenantId && x.ConnectionKey == normalizedKey && x.Issuer == identity.Issuer && x.SubjectHash == subjectHash, cancellationToken);
        return link is null ? null : ToModel(link);
    }

    public async ValueTask<ProvisioningResult> CreateLinkOrGetExistingAsync(ProvisioningRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var existing = await FindLinkAsync(request.TenantId, request.ConnectionKey, request.Identity, cancellationToken);
        if (existing is not null)
            return new ProvisioningResult(existing.UserId, existing, false);

        // Resolved outside the try so a user resolution failure is never mistaken for a link conflict.
        var (user, wasCreated) = await ResolveUserAsync(request, cancellationToken);
        var link = new PersistedExternalIdentityLink
        {
            Id = identityGenerator.GenerateId(),
            TenantId = request.TenantId,
            ConnectionKey = ConnectionRevisionCalculator.NormalizeKey(request.ConnectionKey),
            Issuer = request.Identity.Issuer,
            SubjectHash = handleHasher.Hash(request.Identity.Subject),
            UserId = user.Id,
            CreatedAt = clock.UtcNow
        };

        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            dbContext.ExternalIdentityLinks.Add(link);
            await dbContext.SaveChangesAsync(cancellationToken);
            return new ProvisioningResult(user.Id, ToModel(link), wasCreated, true);
        }
        catch (DbUpdateException)
        {
            // IX_ExternalIdentityLink_Identity arbitrates concurrent first sign-ins for the same identity tuple.
            var winner = await FindLinkAsync(request.TenantId, request.ConnectionKey, request.Identity, cancellationToken);
            if (winner is null)
                throw; // Not a uniqueness conflict, so leave any just-created user in place.
            if (wasCreated)
                await TryRemoveStrandedUserAsync(user, cancellationToken);
            return new ProvisioningResult(winner.UserId, winner, false);
        }
    }

    public async ValueTask<ExternalIdentityLinkReplaceResult> ReplaceAsync(ExternalIdentityLinkReplaceRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var normalizedConnectionKey = ConnectionRevisionCalculator.NormalizeKey(request.ConnectionKey);
        var subjectHash = handleHasher.Hash(request.Identity.Subject);
        ExternalIdentityLink? oldLink = null;

        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var oldEntity = await dbContext.ExternalIdentityLinks.AsNoTracking().SingleOrDefaultAsync(
                x => x.Id == request.LinkId && x.TenantId == request.TenantId,
                cancellationToken);
            if (oldEntity is null)
                return new ExternalIdentityLinkReplaceResult.NotFound();

            oldLink = ToModel(oldEntity);
            var conflictingEntity = await dbContext.ExternalIdentityLinks.AsNoTracking().SingleOrDefaultAsync(
                x => x.Id != request.LinkId &&
                     x.TenantId == request.TenantId &&
                     x.ConnectionKey == normalizedConnectionKey &&
                     x.Issuer == request.Identity.Issuer &&
                     x.SubjectHash == subjectHash,
                cancellationToken);
            if (conflictingEntity is not null)
                return new ExternalIdentityLinkReplaceResult.Conflict(oldLink, ToModel(conflictingEntity));

            // The identity aggregate lives in its own store, so the target user is verified through its contract.
            // Checked here rather than up front to preserve the "unknown link wins over unknown user" result ordering.
            var user = await userProvider.FindAsync(new UserFilter { Id = request.UserId }, cancellationToken);
            if (user is null || !string.Equals(user.TenantId, request.TenantId, StringComparison.Ordinal))
                throw new InvalidOperationException("The requested Elsa user does not exist or is outside the target tenant.");

            var deleted = await dbContext.ExternalIdentityLinks
                .Where(x => x.Id == request.LinkId && x.TenantId == request.TenantId)
                .ExecuteDeleteAsync(cancellationToken);
            if (deleted == 0)
                return new ExternalIdentityLinkReplaceResult.NotFound();

            var replacementEntity = new PersistedExternalIdentityLink
            {
                Id = identityGenerator.GenerateId(),
                TenantId = request.TenantId,
                ConnectionKey = normalizedConnectionKey,
                Issuer = request.Identity.Issuer,
                SubjectHash = subjectHash,
                UserId = user.Id,
                CreatedAt = clock.UtcNow
            };
            dbContext.ExternalIdentityLinks.Add(replacementEntity);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new ExternalIdentityLinkReplaceResult.Success(oldLink, ToModel(replacementEntity));
        }
        catch (DbUpdateConcurrencyException)
        {
            return new ExternalIdentityLinkReplaceResult.NotFound();
        }
        catch (DbUpdateException) when (oldLink is not null)
        {
            var conflictingLink = await FindLinkAsync(request.TenantId, normalizedConnectionKey, request.Identity, cancellationToken);
            if (conflictingLink is not null && !string.Equals(conflictingLink.Id, request.LinkId, StringComparison.Ordinal))
                return new ExternalIdentityLinkReplaceResult.Conflict(oldLink, conflictingLink);
            throw;
        }
    }

    public async ValueTask<Page<ExternalIdentityLink>> FindAsync(ExternalIdentityLinkFilter filter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var query = dbContext.ExternalIdentityLinks.AsNoTracking()
            .Where(x => x.TenantId == filter.TenantId);
        if (filter.UserId is not null)
            query = query.Where(x => x.UserId == filter.UserId);
        if (filter.ConnectionKey is not null)
            query = query.Where(x => x.ConnectionKey == ConnectionRevisionCalculator.NormalizeKey(filter.ConnectionKey));

        var links = await query.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).Select(x => new ExternalIdentityLink(x.Id, x.TenantId, x.ConnectionKey, x.Issuer, x.SubjectHash, x.SubjectHint, x.UserId, x.CreatedAt, x.LastSignedInAt)).ToArrayAsync(cancellationToken);
        return Page.Of<ExternalIdentityLink>(links, links.Length);
    }

    public async ValueTask<bool> DeleteAsync(string tenantId, string linkId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.ExternalIdentityLinks
            .Where(x => x.Id == linkId && x.TenantId == tenantId)
            .ExecuteDeleteAsync(cancellationToken) > 0;
    }

    private async ValueTask<(User User, bool WasCreated)> ResolveUserAsync(ProvisioningRequest request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.ExistingUserId))
        {
            var existingUser = await userProvider.FindAsync(new UserFilter { Id = request.ExistingUserId }, cancellationToken)
                ?? throw new InvalidOperationException("The requested Elsa user does not exist.");
            if (!string.Equals(existingUser.TenantId, request.TenantId, StringComparison.Ordinal))
                throw new InvalidOperationException("The requested Elsa user is outside the target tenant.");
            return (existingUser, false);
        }

        var proposal = request.Proposal ?? throw new InvalidOperationException("A user creation proposal is required for an unlinked external identity.");
        var roleIds = await ResolveRoleIdsAsync(proposal.DefaultRoleIds, cancellationToken);
        var prefix = NormalizeUserNamePrefix(proposal.UserNamePrefix);
        for (var attempt = 0; attempt < MaximumUserNameAttempts; attempt++)
        {
            var name = $"{prefix}-{identityGenerator.GenerateId()}";
            if (await userProvider.FindAsync(new UserFilter { Name = name }, cancellationToken) is not null)
                continue;

            var user = new User
            {
                Id = identityGenerator.GenerateId(),
                Name = name,
                TenantId = request.TenantId,
                HashedPassword = null,
                HashedPasswordSalt = null,
                Roles = roleIds.ToList()
            };
            await userStore.SaveAsync(user, cancellationToken);
            return (user, true);
        }
        throw new InvalidOperationException("A unique Elsa user name could not be reserved for the external identity.");
    }

    private async ValueTask TryRemoveStrandedUserAsync(User user, CancellationToken cancellationToken)
    {
        try
        {
            await userStore.DeleteAsync(new UserFilter { Id = user.Id }, cancellationToken);
        }
        catch (Exception exception)
        {
            // A credential-less user with no link cannot authenticate, so cleanup must never fail the sign-in.
            logger.LogWarning(exception, "Could not remove the just-in-time user {UserId} that lost the external identity link race", user.Id);
        }
    }

    private static ExternalIdentityLink ToModel(PersistedExternalIdentityLink link) => new(link.Id, link.TenantId, link.ConnectionKey, link.Issuer, link.SubjectHash, link.SubjectHint, link.UserId, link.CreatedAt, link.LastSignedInAt);

    private static string NormalizeUserNamePrefix(string prefix)
    {
        var normalized = new string((prefix ?? string.Empty).Trim().Where(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_').ToArray());
        return string.IsNullOrEmpty(normalized) ? "external" : normalized;
    }

    private async ValueTask<IReadOnlyCollection<string>> ResolveRoleIdsAsync(IReadOnlyCollection<string>? roleIds, CancellationToken cancellationToken)
    {
        var requested = (roleIds ?? []).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal).ToArray();
        if (requested.Length == 0)
            return [];
        var found = (await roleProvider.FindByIdsAsync(requested, cancellationToken)).Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        if (!found.SetEquals(requested))
            throw new InvalidOperationException("A configured default role no longer exists.");
        return requested;
    }
}
