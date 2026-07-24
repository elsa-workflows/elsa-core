using Elsa.Common.Models;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.Persistence.EFCore.Modules.Identity;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.Modules.ExternalAuthentication;

/// <summary>Durable database-owned connection store backed by the Identity transaction boundary.</summary>
public sealed class EFCoreIdentityProviderConnectionStore(IDbContextFactory<IdentityElsaDbContext> dbContextFactory) : IIdentityProviderConnectionStore
{
    public async ValueTask<Page<IdentityProviderConnection>> FindAsync(ConnectionFilter filter, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var query = dbContext.IdentityProviderConnections.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(x => x.Key.Contains(filter.Search) || x.DisplayName.Contains(filter.Search));
        if (filter.Scope is { } scope)
            query = query.Where(x => x.TenantId == scope.TenantId);
        if (!string.IsNullOrWhiteSpace(filter.AdapterType))
            query = query.Where(x => x.AdapterType == filter.AdapterType);
        if (filter.IsEnabled is { } isEnabled)
            query = query.Where(x => x.IsEnabled == isEnabled);
        if (filter.IsArchived is { } isArchived)
            query = isArchived ? query.Where(x => x.ArchivedAt != null) : query.Where(x => x.ArchivedAt == null);

        var totalCount = await query.LongCountAsync(cancellationToken);
        var entries = await query.OrderBy(x => x.TenantId).ThenBy(x => x.DisplayOrder).ThenBy(x => x.Key).ToListAsync(cancellationToken);
        return Page.Of(entries.Select(x => x.ToModel()).ToList(), totalCount);
    }

    public async ValueTask<IdentityProviderConnection?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return (await dbContext.IdentityProviderConnections.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken))?.ToModel();
    }

    public async ValueTask<ConnectionMutationResult> CreateAsync(IdentityProviderConnection connection, CancellationToken cancellationToken = default)
    {
        var persisted = connection.ToPersisted();
        persisted.Revision = Math.Max(1, persisted.Revision);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.IdentityProviderConnections.Add(persisted);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return new ConnectionMutationResult.Created(persisted.ToModel());
        }
        catch (DbUpdateException)
        {
            if (await HasKeyAsync(connection.TenantId, connection.Key, cancellationToken))
                return new ConnectionMutationResult.DuplicateKey();
            throw;
        }
    }

    public async ValueTask<ConnectionMutationResult> UpdateAsync(IdentityProviderConnection connection, long expectedRevision, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var persisted = connection.ToPersisted();
        persisted.Revision = checked(expectedRevision + 1);
        dbContext.IdentityProviderConnections.Attach(persisted);
        dbContext.Entry(persisted).State = EntityState.Modified;
        dbContext.Entry(persisted).Property(x => x.Revision).OriginalValue = expectedRevision;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return new ConnectionMutationResult.Updated(persisted.ToModel());
        }
        catch (DbUpdateConcurrencyException)
        {
            var currentRevision = await dbContext.IdentityProviderConnections.AsNoTracking().Where(x => x.Id == connection.Id).Select(x => (long?)x.Revision).SingleOrDefaultAsync(cancellationToken);
            return currentRevision is null ? new ConnectionMutationResult.NotFound() : new ConnectionMutationResult.RevisionConflict(currentRevision.Value);
        }
        catch (DbUpdateException)
        {
            if (await HasKeyAsync(connection.TenantId, connection.Key, cancellationToken, connection.Id))
                return new ConnectionMutationResult.DuplicateKey();
            throw;
        }
    }

    private async Task<bool> HasKeyAsync(string tenantId, string key, CancellationToken cancellationToken, string? exceptId = null)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.IdentityProviderConnections.AnyAsync(x => x.TenantId == tenantId && x.Key == key && (exceptId == null || x.Id != exceptId), cancellationToken);
    }
}
