using Elsa.Common;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.Persistence.EFCore.Modules.Identity;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.Modules.ExternalAuthentication;

public sealed class EFCoreExternalAuthenticationStateStore(ExternalAuthenticationDbContextFactory dbContextFactory, ISystemClock clock) : IExternalAuthenticationStateStore
{
    public async ValueTask PutAsync<T>(string purpose, string handleHash, T value, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
    {
        await using var lease = await dbContextFactory.CreateAsync(cancellationToken);
        var dbContext = lease.DbContext;
        var persisted = value is BrokerTransaction transaction
            ? transaction.ToPersisted(purpose)
            : new PersistedBrokerTransaction
            {
                Purpose = purpose,
                HandleHash = handleHash,
                ClientId = string.Empty,
                CallbackUri = "https://elsa.invalid/state",
                ReturnPath = "/",
                TenantId = string.Empty,
                PkceChallenge = string.Empty,
                ProtectedPayload = System.Text.Encoding.UTF8.GetBytes(ExternalAuthenticationJsonSerializer.Serialize(value))
            };
        persisted.HandleHash = handleHash;
        persisted.ExpiresAt = expiresAt;
        dbContext.ExternalAuthenticationBrokerTransactions.Add(persisted);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async ValueTask<TakeResult<T>> TryTakeAsync<T>(string purpose, string handleHash, CancellationToken cancellationToken = default)
    {
        await using var lease = await dbContextFactory.CreateAsync(cancellationToken);
        var dbContext = lease.DbContext;
        var entry = await dbContext.ExternalAuthenticationBrokerTransactions.AsNoTracking().SingleOrDefaultAsync(x => x.Purpose == purpose && x.HandleHash == handleHash, cancellationToken);
        if (entry is null)
            return new TakeResult<T>.NotFound();
        if (entry.ExpiresAt <= clock.UtcNow)
            return new TakeResult<T>.Expired();

        var consumed = await dbContext.ExternalAuthenticationBrokerTransactions
            .Where(x => x.Purpose == purpose && x.HandleHash == handleHash && x.ConsumedAt == null)
            .ExecuteUpdateAsync(x => x.SetProperty(y => y.ConsumedAt, clock.UtcNow), cancellationToken);
        if (consumed == 0)
            return new TakeResult<T>.AlreadyConsumed();

        var value = typeof(T) == typeof(BrokerTransaction)
            ? (T)(object)entry.ToModel()
            : ExternalAuthenticationJsonSerializer.Deserialize<T>(System.Text.Encoding.UTF8.GetString(entry.ProtectedPayload));
        return new TakeResult<T>.Taken(value);
    }
}

public sealed class EFCoreAuthorizationGrantStore(ExternalAuthenticationDbContextFactory dbContextFactory, ISystemClock clock) : IAuthorizationGrantStore
{
    public async ValueTask SaveAsync(AuthorizationGrant grant, CancellationToken cancellationToken = default)
    {
        await using var lease = await dbContextFactory.CreateAsync(cancellationToken);
        var dbContext = lease.DbContext;
        dbContext.ExternalAuthenticationAuthorizationGrants.Add(grant.ToPersisted());
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async ValueTask<TakeResult<AuthorizationGrant>> TryTakeAsync(string codeHash, CancellationToken cancellationToken = default)
    {
        await using var lease = await dbContextFactory.CreateAsync(cancellationToken);
        var dbContext = lease.DbContext;
        var grant = await dbContext.ExternalAuthenticationAuthorizationGrants.AsNoTracking().SingleOrDefaultAsync(x => x.CodeHash == codeHash, cancellationToken);
        if (grant is null)
            return new TakeResult<AuthorizationGrant>.NotFound();
        if (grant.ExpiresAt <= clock.UtcNow)
            return new TakeResult<AuthorizationGrant>.Expired();

        var consumed = await dbContext.ExternalAuthenticationAuthorizationGrants
            .Where(x => x.CodeHash == codeHash && x.ConsumedAt == null)
            .ExecuteUpdateAsync(x => x.SetProperty(y => y.ConsumedAt, clock.UtcNow), cancellationToken);
        return consumed == 1 ? new TakeResult<AuthorizationGrant>.Taken(grant.ToModel()) : new TakeResult<AuthorizationGrant>.AlreadyConsumed();
    }
}

public sealed class EFCorePreviewResultStore(ExternalAuthenticationDbContextFactory dbContextFactory, ISystemClock clock) : IPreviewResultStore
{
    public async ValueTask SaveAsync(PreviewResult result, CancellationToken cancellationToken = default)
    {
        await using var lease = await dbContextFactory.CreateAsync(cancellationToken);
        var dbContext = lease.DbContext;
        dbContext.ExternalAuthenticationPreviewResults.Add(result.ToPersisted());
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async ValueTask<TakeResult<PreviewResult>> TryTakeAsync(string handleHash, string administratorId, CancellationToken cancellationToken = default)
    {
        await using var lease = await dbContextFactory.CreateAsync(cancellationToken);
        var dbContext = lease.DbContext;
        var result = await dbContext.ExternalAuthenticationPreviewResults.AsNoTracking().SingleOrDefaultAsync(x => x.HandleHash == handleHash && x.AdministratorId == administratorId, cancellationToken);
        if (result is null)
            return new TakeResult<PreviewResult>.NotFound();
        if (result.ExpiresAt <= clock.UtcNow)
            return new TakeResult<PreviewResult>.Expired();

        var consumed = await dbContext.ExternalAuthenticationPreviewResults
            .Where(x => x.HandleHash == handleHash && x.AdministratorId == administratorId && x.ConsumedAt == null)
            .ExecuteUpdateAsync(x => x.SetProperty(y => y.ConsumedAt, clock.UtcNow), cancellationToken);
        return consumed == 1 ? new TakeResult<PreviewResult>.Taken(result.ToModel()) : new TakeResult<PreviewResult>.AlreadyConsumed();
    }
}

public sealed class EFCoreConnectionObservationStore(ExternalAuthenticationDbContextFactory dbContextFactory) : IConnectionObservationStore
{
    public async ValueTask<ConnectionObservation?> FindLatestAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        await using var lease = await dbContextFactory.CreateAsync(cancellationToken);
        var dbContext = lease.DbContext;
        var observation = await dbContext.ExternalAuthenticationConnectionObservations.AsNoTracking().SingleOrDefaultAsync(x => x.ConnectionId == connectionId, cancellationToken);
        return observation is null ? null : new ConnectionObservation(observation.ConnectionId, observation.TestedMaterialRevision, observation.ObservedAt, (ConnectionObservationStatus)observation.Status, observation.Category, TimeSpan.FromTicks(observation.DurationTicks), observation.Summary, ExternalAuthenticationJsonSerializer.Deserialize<string[]>(observation.WarningsJson), observation.CorrelationId);
    }

    public async ValueTask SaveLatestAsync(ConnectionObservation observation, CancellationToken cancellationToken = default)
    {
        await using var lease = await dbContextFactory.CreateAsync(cancellationToken);
        var dbContext = lease.DbContext;
        var existing = await dbContext.ExternalAuthenticationConnectionObservations.SingleOrDefaultAsync(x => x.ConnectionId == observation.ConnectionId, cancellationToken);
        if (existing is not null && existing.ObservedAt > observation.ObservedAt)
            return;

        var persisted = new PersistedConnectionObservation
        {
            ConnectionId = observation.ConnectionId, TestedMaterialRevision = observation.TestedMaterialRevision, ObservedAt = observation.ObservedAt,
            Status = (int)observation.Status, Category = observation.Category, DurationTicks = observation.Duration.Ticks, Summary = observation.Summary,
            WarningsJson = ExternalAuthenticationJsonSerializer.Serialize(observation.Warnings), CorrelationId = observation.CorrelationId
        };
        if (existing is null)
            dbContext.ExternalAuthenticationConnectionObservations.Add(persisted);
        else
            dbContext.Entry(existing).CurrentValues.SetValues(persisted);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public sealed class EFCoreConnectionRegistryVersionStore(ExternalAuthenticationDbContextFactory dbContextFactory) : IConnectionRegistryVersionStore
{
    private const int SingletonId = 1;

    public async ValueTask<long> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        await using var lease = await dbContextFactory.CreateAsync(cancellationToken);
        var dbContext = lease.DbContext;
        return await dbContext.ExternalAuthenticationRegistryVersions.Where(x => x.Id == SingletonId).Select(x => (long?)x.Version).SingleOrDefaultAsync(cancellationToken) ?? 1;
    }

    public async ValueTask<long> AdvanceAsync(CancellationToken cancellationToken = default)
    {
        await using var lease = await dbContextFactory.CreateAsync(cancellationToken);
        var dbContext = lease.DbContext;
        var changed = await dbContext.ExternalAuthenticationRegistryVersions.Where(x => x.Id == SingletonId)
            .ExecuteUpdateAsync(x => x.SetProperty(y => y.Version, y => y.Version + 1), cancellationToken);
        if (changed == 0)
        {
            dbContext.ExternalAuthenticationRegistryVersions.Add(new ExternalAuthenticationRegistryVersion { Id = SingletonId, Version = 2 });
            try { await dbContext.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateException) { return await AdvanceAsync(cancellationToken); }
            return 2;
        }
        return await GetVersionAsync(cancellationToken);
    }

    public async ValueTask<bool> IsCurrentAsync(long version, CancellationToken cancellationToken = default) => await GetVersionAsync(cancellationToken) == version;
}
