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
    ILogger<EFCoreExternalIdentityProvisioner> logger) : IExternalIdentityProvisioner, IExternalIdentityLinkManagementStore, IExternalIdentitySignInTracker
{
    private readonly ExternalIdentityUserProvisioningService _userProvisioningService = new(userStore, userProvider, roleProvider, identityGenerator);

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
        var (user, wasCreated) = await _userProvisioningService.ResolveAsync(request, cancellationToken: cancellationToken);
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

        var saveAttempted = false;
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            dbContext.ExternalIdentityLinks.Add(link);
            saveAttempted = true;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException linkException)
        {
            // IX_ExternalIdentityLink_Identity arbitrates concurrent first sign-ins for the same identity tuple.
            var winner = await FindLinkAsync(request.TenantId, request.ConnectionKey, request.Identity, CancellationToken.None);
            if (winner?.Id == link.Id)
            {
                await EnsureLinkedUserStillExistsAsync(link, user, wasCreated, CancellationToken.None);
                return new ProvisioningResult(user.Id, winner, wasCreated, true);
            }
            if (wasCreated)
                await RemoveStrandedUserAsync(user, linkException, CancellationToken.None);
            if (winner is null)
                throw;
            return new ProvisioningResult(winner.UserId, winner, false);
        }
        catch (Exception linkException)
        {
            if (saveAttempted && await LinkExistsAsync(link.Id, CancellationToken.None))
                await EnsureLinkedUserStillExistsAsync(link, user, wasCreated, CancellationToken.None);
            else if (wasCreated)
                await RemoveStrandedUserAsync(user, linkException, CancellationToken.None);

            throw;
        }

        // Once the link is durable, cancellation must not interrupt the complementary user check and cleanup.
        await EnsureLinkedUserStillExistsAsync(link, user, wasCreated, CancellationToken.None);
        return new ProvisioningResult(user.Id, ToModel(link), wasCreated, true);
    }

    public async ValueTask<bool> RecordSuccessfulSignInAsync(
        string tenantId,
        string connectionKey,
        ExternalIdentity identity,
        string userId,
        DateTimeOffset signedInAt,
        CancellationToken cancellationToken = default)
    {
        var subjectHash = handleHasher.Hash(identity.Subject);
        var normalizedConnectionKey = ConnectionRevisionCalculator.NormalizeKey(connectionKey);
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var linkQuery = dbContext.ExternalIdentityLinks
            .Where(x =>
                x.TenantId == tenantId &&
                x.ConnectionKey == normalizedConnectionKey &&
                x.Issuer == identity.Issuer &&
                x.SubjectHash == subjectHash &&
                x.UserId == userId);
        while (true)
        {
            var link = await linkQuery.AsNoTracking().SingleOrDefaultAsync(cancellationToken);
            if (link is null)
                return false;
            if (link.LastSignedInAt >= signedInAt)
                return true;

            var previousSignedInAt = link.LastSignedInAt;
            var updated = await linkQuery
                .Where(x => x.LastSignedInAt == previousSignedInAt)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.LastSignedInAt, (DateTimeOffset?)signedInAt), cancellationToken);
            if (updated > 0)
                return true;
        }
    }

    public async ValueTask<ExternalIdentityLinkReplaceResult> ReplaceAsync(ExternalIdentityLinkReplaceRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var normalizedConnectionKey = ConnectionRevisionCalculator.NormalizeKey(request.ConnectionKey);
        var subjectHash = handleHasher.Hash(request.Identity.Subject);
        ExternalIdentityLink? oldLink = null;
        PersistedExternalIdentityLink? oldEntity = null;
        PersistedExternalIdentityLink? replacementEntity = null;
        User? replacementUser = null;
        var commitAttempted = false;
        var replacementCommitted = false;

        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            oldEntity = await dbContext.ExternalIdentityLinks.AsNoTracking().SingleOrDefaultAsync(
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
            (replacementUser, _) = await _userProvisioningService.ResolveAsync(
                new ProvisioningRequest(request.TenantId, normalizedConnectionKey, request.Identity, null, request.UserId),
                cancellationToken: cancellationToken);

            var deleted = await dbContext.ExternalIdentityLinks
                .Where(x => x.Id == request.LinkId && x.TenantId == request.TenantId)
                .ExecuteDeleteAsync(cancellationToken);
            if (deleted == 0)
                return new ExternalIdentityLinkReplaceResult.NotFound();

            replacementEntity = new PersistedExternalIdentityLink
            {
                Id = identityGenerator.GenerateId(),
                TenantId = request.TenantId,
                ConnectionKey = normalizedConnectionKey,
                Issuer = request.Identity.Issuer,
                SubjectHash = subjectHash,
                UserId = replacementUser.Id,
                CreatedAt = clock.UtcNow
            };
            dbContext.ExternalIdentityLinks.Add(replacementEntity);
            await dbContext.SaveChangesAsync(cancellationToken);
            commitAttempted = true;
            // The replacement write has succeeded; cancellation must not make commit outcome ambiguous.
            await transaction.CommitAsync(CancellationToken.None);
            replacementCommitted = true;

            await EnsureReplacementUserStillExistsAsync(oldEntity, replacementEntity, replacementUser, CancellationToken.None);
            return new ExternalIdentityLinkReplaceResult.Success(oldLink, ToModel(replacementEntity));
        }
        catch (DbUpdateConcurrencyException) when (!commitAttempted)
        {
            return new ExternalIdentityLinkReplaceResult.NotFound();
        }
        catch (DbUpdateException) when (!commitAttempted && oldLink is not null)
        {
            var conflictingLink = await FindLinkAsync(request.TenantId, normalizedConnectionKey, request.Identity, CancellationToken.None);
            if (conflictingLink is not null && !string.Equals(conflictingLink.Id, request.LinkId, StringComparison.Ordinal))
                return new ExternalIdentityLinkReplaceResult.Conflict(oldLink, conflictingLink);
            throw;
        }
        catch (Exception) when (commitAttempted && !replacementCommitted && oldLink is not null && oldEntity is not null && replacementEntity is not null && replacementUser is not null)
        {
            // The transaction scope has been disposed before this handler runs, so probing cannot contend with it.
            var durableLink = await FindLinkAsync(request.TenantId, normalizedConnectionKey, request.Identity, CancellationToken.None);
            if (durableLink?.Id != replacementEntity.Id)
                throw;

            replacementCommitted = true;
            await EnsureReplacementUserStillExistsAsync(oldEntity, replacementEntity, replacementUser, CancellationToken.None);
            return new ExternalIdentityLinkReplaceResult.Success(oldLink, durableLink);
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

    private async ValueTask EnsureLinkedUserStillExistsAsync(
        PersistedExternalIdentityLink link,
        User user,
        bool wasCreated,
        CancellationToken cancellationToken)
    {
        if (await _userProvisioningService.ExistsAsync(user, wasCreated, cancellationToken))
            return;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await dbContext.ExternalIdentityLinks.Where(x => x.Id == link.Id).ExecuteDeleteAsync(cancellationToken);
        throw new InvalidOperationException("The Elsa user was deleted while its external identity link was being created.");
    }

    private async ValueTask EnsureReplacementUserStillExistsAsync(
        PersistedExternalIdentityLink oldLink,
        PersistedExternalIdentityLink replacementLink,
        User replacementUser,
        CancellationToken cancellationToken)
    {
        if (await _userProvisioningService.ExistsAsync(replacementUser, false, cancellationToken))
            return;

        await CompensateReplacementAsync(oldLink, replacementLink, cancellationToken);
        throw new InvalidOperationException("The Elsa user was deleted while its external identity link was being replaced.");
    }

    private async ValueTask CompensateReplacementAsync(
        PersistedExternalIdentityLink oldLink,
        PersistedExternalIdentityLink replacementLink,
        CancellationToken cancellationToken)
    {
        var commitAttempted = false;
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            await dbContext.ExternalIdentityLinks.Where(x => x.Id == replacementLink.Id).ExecuteDeleteAsync(cancellationToken);
            dbContext.ExternalIdentityLinks.Add(new PersistedExternalIdentityLink
            {
                Id = oldLink.Id,
                TenantId = oldLink.TenantId,
                ConnectionKey = oldLink.ConnectionKey,
                Issuer = oldLink.Issuer,
                SubjectHash = oldLink.SubjectHash,
                SubjectHint = oldLink.SubjectHint,
                UserId = oldLink.UserId,
                CreatedAt = oldLink.CreatedAt,
                LastSignedInAt = oldLink.LastSignedInAt
            });
            await dbContext.SaveChangesAsync(cancellationToken);
            commitAttempted = true;
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception compensationException)
        {
            // The transaction scope has been disposed before this handler runs. A lost commit acknowledgement must
            // not turn a successfully restored previous link into data loss.
            var restorationCommitted = commitAttempted &&
                                       await LinkExistsAsync(oldLink.Id, cancellationToken) &&
                                       !await LinkExistsAsync(replacementLink.Id, cancellationToken);
            if (!restorationCommitted)
            {
                await RemoveReplacementLinksOrThrowAsync(oldLink.Id, replacementLink.Id, compensationException, cancellationToken);
                throw new InvalidOperationException(
                    "The replacement link was removed after its target user was deleted, but the previous link could not be restored.",
                    compensationException);
            }
        }

        // An indeterminate user-directory failure must not be mistaken for a failed link restoration.
        // Only remove the restored link when the directory positively reports that its user is gone.
        var previousUser = new User { Id = oldLink.UserId, TenantId = oldLink.TenantId };
        if (!await _userProvisioningService.ExistsAsync(previousUser, false, cancellationToken))
        {
            try
            {
                await using var cleanupContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
                await cleanupContext.ExternalIdentityLinks.Where(x => x.Id == oldLink.Id).ExecuteDeleteAsync(cancellationToken);
            }
            catch (Exception cleanupException)
            {
                await RemoveReplacementLinksOrThrowAsync(oldLink.Id, replacementLink.Id, cleanupException, cancellationToken);
                throw new InvalidOperationException(
                    "The restored previous link was removed after its user was deleted, but its first cleanup attempt failed.",
                    cleanupException);
            }
        }
    }

    private async ValueTask<bool> LinkExistsAsync(string linkId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.ExternalIdentityLinks.AsNoTracking().AnyAsync(x => x.Id == linkId, cancellationToken);
    }

    private async ValueTask RemoveReplacementLinksOrThrowAsync(
        string oldLinkId,
        string replacementLinkId,
        Exception operationException,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var cleanupContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await cleanupContext.ExternalIdentityLinks
                .Where(x => x.Id == replacementLinkId || x.Id == oldLinkId)
                .ExecuteDeleteAsync(cancellationToken);
        }
        catch (Exception cleanupException)
        {
            throw new AggregateException(
                "A replacement-compensation link refers to a deleted user and could not be removed. No credentials were issued.",
                operationException,
                cleanupException);
        }
    }

    private async ValueTask RemoveStrandedUserAsync(User user, Exception linkException, CancellationToken cancellationToken)
    {
        try
        {
            await _userProvisioningService.RemoveAsync(user, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not remove the just-in-time user {UserId} after its external identity link failed", user.Id);
            throw new AggregateException(
                "External identity provisioning failed and its just-in-time user could not be removed. No credentials were issued.",
                linkException,
                exception);
        }
    }

    private static ExternalIdentityLink ToModel(PersistedExternalIdentityLink link) => new(link.Id, link.TenantId, link.ConnectionKey, link.Issuer, link.SubjectHash, link.SubjectHint, link.UserId, link.CreatedAt, link.LastSignedInAt);

}
