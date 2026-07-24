using Elsa.Common;
using Elsa.Common.Models;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.Identity.Entities;
using Elsa.Persistence.EFCore.Modules.Identity;
using Elsa.Workflows;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.Modules.ExternalAuthentication;

/// <summary>Creates users and external links in one database transaction, converging safely on the durable unique link.</summary>
public sealed class EFCoreExternalIdentityProvisioner(
    IDbContextFactory<IdentityElsaDbContext> dbContextFactory,
    IExternalAuthenticationHandleHasher handleHasher,
    IIdentityGenerator identityGenerator,
    ISystemClock clock) : IExternalIdentityProvisioner, IExternalIdentityLinkManagementStore
{
    private const int MaximumUserNameAttempts = 10;

    public async ValueTask<ExternalIdentityLink?> FindLinkAsync(string tenantId, string connectionId, ExternalIdentity identity, CancellationToken cancellationToken = default)
    {
        var subjectHash = handleHasher.Hash(identity.Subject);
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var link = await dbContext.ExternalIdentityLinks.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenantId && x.ConnectionId == connectionId && x.Issuer == identity.Issuer && x.SubjectHash == subjectHash, cancellationToken);
        return link is null ? null : ToModel(link);
    }

    public async ValueTask<ProvisioningResult> CreateLinkOrGetExistingAsync(ProvisioningRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await FindLinkAsync(request.TenantId, request.ConnectionId, request.Identity, cancellationToken);
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
                ConnectionId = request.ConnectionId,
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
            existing = await FindLinkAsync(request.TenantId, request.ConnectionId, request.Identity, cancellationToken);
            if (existing is not null)
                return new ProvisioningResult(existing.UserId, existing, false);
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
        if (filter.ConnectionId is not null)
            query = query.Where(x => x.ConnectionId == filter.ConnectionId);

        var links = await query.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).Select(x => new ExternalIdentityLink(x.Id, x.TenantId, x.ConnectionId, x.Issuer, x.SubjectHash, x.SubjectHint, x.UserId, x.CreatedAt, x.LastSignedInAt)).ToArrayAsync(cancellationToken);
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
        var prefix = NormalizeUserNamePrefix(proposal.UserNamePrefix);
        for (var attempt = 0; attempt < MaximumUserNameAttempts; attempt++)
        {
            var name = $"{prefix}-{identityGenerator.GenerateId()}";
            if (await dbContext.Users.AnyAsync(x => x.Name == name, cancellationToken))
                continue;
            var user = new User { Id = identityGenerator.GenerateId(), Name = name, TenantId = request.TenantId };
            dbContext.Users.Add(user);
            return (user, true);
        }
        throw new InvalidOperationException("A unique Elsa user name could not be reserved for the external identity.");
    }

    private static ExternalIdentityLink ToModel(PersistedExternalIdentityLink link) => new(link.Id, link.TenantId, link.ConnectionId, link.Issuer, link.SubjectHash, link.SubjectHint, link.UserId, link.CreatedAt, link.LastSignedInAt);

    private static string NormalizeUserNamePrefix(string prefix)
    {
        var normalized = new string((prefix ?? string.Empty).Trim().Where(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_').ToArray());
        return string.IsNullOrEmpty(normalized) ? "external" : normalized;
    }
}
