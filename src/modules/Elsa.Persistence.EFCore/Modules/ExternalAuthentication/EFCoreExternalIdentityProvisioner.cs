using Elsa.Common;
using Elsa.Common.Models;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Services;
using Elsa.Identity.Entities;
using Elsa.Extensions;
using Elsa.Persistence.EFCore.Modules.Identity;
using Elsa.Workflows;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.Modules.ExternalAuthentication;

/// <summary>Creates users and external links in one database transaction, converging safely on the durable unique link.</summary>
public sealed class EFCoreExternalIdentityProvisioner(
    IDbContextFactory<IdentityElsaDbContext> dbContextFactory,
    Elsa.Identity.Contracts.IRoleProvider roleProvider,
    IExternalAuthenticationHandleHasher handleHasher,
    IIdentityGenerator identityGenerator,
    ISystemClock clock) : IExternalIdentityProvisioner, IExternalIdentityLinkManagementStore
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
        var existing = await FindLinkAsync(request.TenantId, request.ConnectionKey, request.Identity, cancellationToken);
        if (existing is not null)
            return new ProvisioningResult(existing.UserId, existing, false);

        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var user = await ResolveUserAsync(dbContext, request, cancellationToken);
            var link = new PersistedExternalIdentityLink
            {
                Id = identityGenerator.GenerateId(),
                TenantId = request.TenantId,
                ConnectionKey = ConnectionRevisionCalculator.NormalizeKey(request.ConnectionKey),
                Issuer = request.Identity.Issuer,
                SubjectHash = handleHasher.Hash(request.Identity.Subject),
                UserId = user.User.Id,
                CreatedAt = clock.UtcNow
            };
            dbContext.ExternalIdentityLinks.Add(link);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new ProvisioningResult(user.User.Id, ToModel(link), user.WasCreated, true);
        }
        catch (DbUpdateException)
        {
            existing = await FindLinkAsync(request.TenantId, request.ConnectionKey, request.Identity, cancellationToken);
            if (existing is not null)
                return new ProvisioningResult(existing.UserId, existing, false);
            throw;
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

            var user = await dbContext.Users.SingleOrDefaultAsync(
                x => x.Id == request.UserId && x.TenantId == request.TenantId,
                cancellationToken);
            if (user is null)
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

    private async Task<(User User, bool WasCreated)> ResolveUserAsync(IdentityElsaDbContext dbContext, ProvisioningRequest request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.ExistingUserId))
        {
            var existingUser = await dbContext.Users.SingleOrDefaultAsync(x => x.Id == request.ExistingUserId, cancellationToken)
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
            if (await dbContext.Users.AnyAsync(x => x.Name == name, cancellationToken))
                continue;
            var user = new User { Id = identityGenerator.GenerateId(), Name = name, TenantId = request.TenantId, Roles = roleIds.ToList() };
            dbContext.Users.Add(user);
            return (user, true);
        }
        throw new InvalidOperationException("A unique Elsa user name could not be reserved for the external identity.");
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
