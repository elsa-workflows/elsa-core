using System.Security.Cryptography;
using System.Text;
using Elsa.Common;
using Elsa.Common.DistributedHosting;
using Elsa.Common.RecurringTasks;
using JetBrains.Annotations;
using Medallion.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Elsa.Persistence.EFCore;

/// <summary>
/// Executes EF Core migrations using the specified <see cref="DbContext"/> type.
/// </summary>
[UsedImplicitly]
[Order(-100)]
public class RunMigrationsStartupTask<TDbContext> : IStartupTask where TDbContext : DbContext
{
    private readonly IDbContextFactory<TDbContext> _dbContextFactory;
    private readonly IOptions<MigrationOptions> _options;
    private readonly MigratedDatabaseRegistry? _migratedDatabaseRegistry;
    private readonly IDistributedLockProvider? _distributedLockProvider;
    private readonly IOptions<DistributedLockingOptions>? _lockingOptions;

    /// <summary>
    /// Executes EF Core migrations using the specified <see cref="DbContext"/> type.
    /// </summary>
    public RunMigrationsStartupTask(
        IDbContextFactory<TDbContext> dbContextFactory,
        IOptions<MigrationOptions> options,
        MigratedDatabaseRegistry? migratedDatabaseRegistry,
        IDistributedLockProvider? distributedLockProvider,
        IOptions<DistributedLockingOptions>? lockingOptions)
    {
        _dbContextFactory = dbContextFactory;
        _options = options;
        _migratedDatabaseRegistry = migratedDatabaseRegistry;
        _distributedLockProvider = distributedLockProvider;
        _lockingOptions = lockingOptions;
    }

    /// <summary>
    /// Executes EF Core migrations without a distributed lock.
    /// </summary>
    [Obsolete("Use the constructor that accepts an IDistributedLockProvider instead. Removed in the next major version.")]
    public RunMigrationsStartupTask(IDbContextFactory<TDbContext> dbContextFactory, IOptions<MigrationOptions> options, MigratedDatabaseRegistry? migratedDatabaseRegistry = null)
        : this(dbContextFactory, options, migratedDatabaseRegistry, null, null)
    {
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _options.Value.RunMigrations.TryGetValue(typeof(TDbContext), out bool shouldRunMigrations);

        if (!shouldRunMigrations)
            return;

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var connectionString = dbContext.Database.IsRelational() ? dbContext.Database.GetConnectionString() : null;

        if (_migratedDatabaseRegistry == null)
        {
            await MigrateAsync(dbContext, connectionString, cancellationToken);
            return;
        }

        await _migratedDatabaseRegistry.MigrateOnceAsync(typeof(TDbContext), connectionString, ct => MigrateAsync(dbContext, connectionString, ct), cancellationToken);
    }

    // The distributed lock guards other nodes. It is taken per database, so tenants with their own database do not queue behind each other.
    private async Task MigrateAsync(TDbContext dbContext, string? connectionString, CancellationToken cancellationToken)
    {
        if (_distributedLockProvider == null)
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
            return;
        }

        var timeout = (_lockingOptions?.Value ?? new DistributedLockingOptions()).LockAcquisitionTimeout;

        await using (await _distributedLockProvider.AcquireLockAsync(GetLockName(connectionString), timeout, cancellationToken))
            await dbContext.Database.MigrateAsync(cancellationToken);
    }

    // The connection string may hold secrets and a lock name may become a file name, so only a hash of it goes into the name.
    private static string GetLockName(string? connectionString)
    {
        var name = typeof(TDbContext).AssemblyQualifiedName!;

        if (string.IsNullOrEmpty(connectionString))
            return name;

        return $"{name}:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(connectionString)))}";
    }
}
