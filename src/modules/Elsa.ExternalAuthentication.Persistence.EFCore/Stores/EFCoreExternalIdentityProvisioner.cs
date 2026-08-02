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

        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            dbContext.ExternalIdentityLinks.Add(link);
            await dbContext.SaveChangesAsync(cancellationToken);
            await EnsureLinkedUserStillExistsAsync(dbContext, link, user, wasCreated, cancellationToken);
            return new ProvisioningResult(user.Id, ToModel(link), wasCreated, true);
        }
        catch (DbUpdateException linkException)
        {
            // IX_ExternalIdentityLink_Identity arbitrates concurrent first sign-ins for the same identity tuple.
            var winner = await FindLinkAsync(request.TenantId, request.ConnectionKey, request.Identity, cancellationToken);
            if (winner?.Id == link.Id)
                return new ProvisioningResult(user.Id, winner, wasCreated, true);
            if (wasCreated)
                await RemoveStrandedUserAsync(user, linkException, cancellationToken);
            if (winner is null)
                throw;
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
            var (user, _) = await _userProvisioningService.ResolveAsync(
                new ProvisioningRequest(request.TenantId, normalizedConnectionKey, request.Identity, null, request.UserId),
                cancellationToken: cancellationToken);

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
            if (!await _userProvisioningService.ExistsAsync(user, false, cancellationToken))
            {
                await CompensateReplacementAsync(oldEntity, replacementEntity, cancellationToken);
                throw new InvalidOperationException("The Elsa user was deleted while its external identity link was being replaced.");
            }
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

    private async ValueTask EnsureLinkedUserStillExistsAsync(
        ExternalAuthenticationElsaDbContext dbContext,
        PersistedExternalIdentityLink link,
        User user,
        bool wasCreated,
        CancellationToken cancellationToken)
    {
        if (await _userProvisioningService.ExistsAsync(user, wasCreated, cancellationToken))
            return;

        await dbContext.ExternalIdentityLinks.Where(x => x.Id == link.Id).ExecuteDeleteAsync(cancellationToken);
        throw new InvalidOperationException("The Elsa user was deleted while its external identity link was being created.");
    }

    private async ValueTask CompensateReplacementAsync(
        PersistedExternalIdentityLink oldLink,
        PersistedExternalIdentityLink replacementLink,
        CancellationToken cancellationToken)
    {
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
            await transaction.CommitAsync(cancellationToken);

            var previousUser = new User { Id = oldLink.UserId, TenantId = oldLink.TenantId };
            if (!await _userProvisioningService.ExistsAsync(previousUser, false, cancellationToken))
            {
                await using var cleanupContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
                await cleanupContext.ExternalIdentityLinks.Where(x => x.Id == oldLink.Id).ExecuteDeleteAsync(cancellationToken);
            }
        }
        catch (Exception compensationException)
        {
            try
            {
                await using var cleanupContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
                await cleanupContext.ExternalIdentityLinks
                    .Where(x => x.Id == replacementLink.Id || x.Id == oldLink.Id)
                    .ExecuteDeleteAsync(cancellationToken);
            }
            catch (Exception cleanupException)
            {
                throw new AggregateException(
                    "A replacement-compensation link refers to a deleted user and could not be removed. No credentials were issued.",
                    compensationException,
                    cleanupException);
            }

            throw new InvalidOperationException(
                "The replacement link was removed after its target user was deleted, but the previous link could not be restored.",
                compensationException);
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
