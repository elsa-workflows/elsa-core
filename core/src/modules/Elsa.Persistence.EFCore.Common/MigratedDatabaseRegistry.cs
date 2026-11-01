using System.Collections.Concurrent;

// ReSharper disable once CheckNamespace
namespace Elsa.Persistence.EFCore;

/// <summary>
/// Keeps track of the databases that have already been migrated by this process, so that migrations are applied once
/// per database rather than once per tenant.
/// </summary>
public class MigratedDatabaseRegistry
{
    private readonly ConcurrentDictionary<(Type DbContextType, string ConnectionString), DatabaseMigration> _migrations = new();

    /// <summary>
    /// Runs <paramref name="migrate"/> unless the database has already been migrated by this process. A caller that arrives
    /// while another caller is migrating the same database waits for it and then skips. If that migration fails, the next
    /// caller migrates. A database without a connection string cannot be told apart from any other, so it is always migrated.
    /// </summary>
    public async Task MigrateOnceAsync(Type dbContextType, string? connectionString, Func<CancellationToken, Task> migrate, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            await migrate(cancellationToken);
            return;
        }

        var migration = _migrations.GetOrAdd((dbContextType, connectionString), _ => new DatabaseMigration());

        if (migration.IsMigrated)
            return;

        await migration.Gate.WaitAsync(cancellationToken);

        try
        {
            if (migration.IsMigrated)
                return;

            await migrate(cancellationToken);
            migration.IsMigrated = true;
        }
        finally
        {
            migration.Gate.Release();
        }
    }

    /// <summary>
    /// Returns <c>true</c> for the first caller to claim a database, and <c>false</c> for every caller after that, or while
    /// <see cref="MigrateOnceAsync"/> is migrating it. A database without a connection string cannot be told apart from any
    /// other, so it is never claimed.
    /// </summary>
    [Obsolete("Use MigrateOnceAsync instead, which also makes concurrent callers wait for the migration. Removed in the next major version.")]
    public bool TryClaim(Type dbContextType, string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return true;

        var migration = _migrations.GetOrAdd((dbContextType, connectionString), _ => new DatabaseMigration());

        lock (migration)
        {
            if (migration.IsMigrated || migration.Gate.CurrentCount == 0)
                return false;

            migration.IsMigrated = true;
            return true;
        }
    }

    /// <summary>
    /// Gives up a claim so that a later attempt can migrate the database after a failed one.
    /// </summary>
    [Obsolete("Use MigrateOnceAsync instead, which retries after a failed migration. Removed in the next major version.")]
    public void Release(Type dbContextType, string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return;

        if (_migrations.TryGetValue((dbContextType, connectionString), out var migration))
        {
            lock (migration)
                migration.IsMigrated = false;
        }
    }

    private sealed class DatabaseMigration
    {
        public SemaphoreSlim Gate { get; } = new(1, 1);
        public volatile bool IsMigrated;
    }
}
