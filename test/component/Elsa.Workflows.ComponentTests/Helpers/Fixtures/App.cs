using Elsa.Workflows.ComponentTests.Services;
using Microsoft.Data.SqlClient;
using TUnit.Core.Interfaces;

namespace Elsa.Workflows.ComponentTests.Fixtures;

/// <summary>
/// Owns every mutable resource used by one expanded TUnit test invocation.
/// </summary>
public sealed class App : IAsyncInitializer, IAsyncDisposable
{
    private readonly Lock _lifecycleLock = new();
    private ComponentTestWebApplicationFactory? _rootFactory;
    private ComponentTestCatalog? _catalog;
    private Task<Cluster>? _clusterTask;
    private Task? _disposeTask;

    [ClassDataSource<Infrastructure>(Shared = SharedType.PerTestSession)]
    public required Infrastructure Infrastructure { get; init; }

    public string CatalogName { get; private set; } = string.Empty;
    public string ConnectionString { get; private set; } = string.Empty;
    public string TestRoot { get; private set; } = string.Empty;
    internal ComponentTestHostOptions HostOptions { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var testContext = TestContext.Current
            ?? throw new InvalidOperationException("A current TUnit test context is required to initialize component resources.");

        CatalogName = testContext.Isolation.GetIsolatedName("elsa_component");
        TestRoot = Path.Combine(
            Path.GetTempPath(),
            testContext.Isolation.GetIsolatedName($"elsa_component_files_{testContext.Id.Replace("-", string.Empty, StringComparison.Ordinal)}"));

        var catalogBuilder = new SqlConnectionStringBuilder(Infrastructure.DbContainer.GetConnectionString())
        {
            InitialCatalog = CatalogName,
            // Elsa's ambient commit transaction opens sequential EF connections. Pool reuse keeps
            // those enlistments promotable on platforms without MSDTC support.
            Pooling = true
        };
        ConnectionString = catalogBuilder.ConnectionString;
        var masterConnectionString = new SqlConnectionStringBuilder(Infrastructure.DbContainer.GetConnectionString())
        {
            InitialCatalog = "master",
            Pooling = false
        }.ConnectionString;
        _catalog = new ComponentTestCatalog(CatalogName, ConnectionString, masterConnectionString);

        try
        {
            var scenarioDirectory = Path.Combine(AppContext.BaseDirectory, "Scenarios");
            var lockDirectory = Path.Combine(TestRoot, "locks");
            var httpFileCacheDirectory = Path.Combine(TestRoot, "http-file-cache");
            if (!Directory.Exists(scenarioDirectory))
                throw new DirectoryNotFoundException($"Component scenario directory '{scenarioDirectory}' was not found.");
            Directory.CreateDirectory(lockDirectory);
            Directory.CreateDirectory(httpFileCacheDirectory);

            HostOptions = new ComponentTestHostOptions(
                ConnectionString,
                scenarioDirectory,
                lockDirectory,
                httpFileCacheDirectory,
                new WorkflowExecutionTracker(),
                _catalog);
        }
        catch (Exception initializationFailure)
        {
            try
            {
                await DisposeAsync();
            }
            catch (Exception cleanupFailure)
            {
                throw new AggregateException("Component app initialization and cleanup both failed.", initializationFailure, cleanupFailure);
            }

            throw;
        }
    }

    internal async Task<Cluster> StartAsync(TestContext testContext)
    {
        Task<Cluster> clusterTask;
        lock (_lifecycleLock)
        {
            ObjectDisposedException.ThrowIf(_disposeTask is not null, this);
            var rootFactory = _rootFactory ??= new ComponentTestWebApplicationFactory();
            clusterTask = _clusterTask ??= StartClusterCoreAsync(rootFactory, HostOptions, testContext);
        }

        return await clusterTask;
    }

    public ValueTask DisposeAsync()
    {
        lock (_lifecycleLock)
        {
            _disposeTask ??= DisposeCoreAsync(_clusterTask, _rootFactory, _catalog);
            return new ValueTask(_disposeTask);
        }
    }

    private async Task DisposeCoreAsync(
        Task<Cluster>? clusterTask,
        ComponentTestWebApplicationFactory? rootFactory,
        ComponentTestCatalog? catalog)
    {
        Cluster? cluster = null;
        List<Exception>? failures = null;

        if (clusterTask is not null)
        {
            try
            {
                cluster = await clusterTask;
            }
            catch (Exception exception)
            {
                (failures ??= []).Add(exception);
            }
        }

        var rootFactoryDisposed = rootFactory is null;

        if (cluster is not null)
        {
            try
            {
                await cluster.DisposeAsync();
            }
            catch (Exception exception)
            {
                (failures ??= []).Add(exception);
            }
        }

        if (rootFactory is not null)
        {
            try
            {
                // ASP.NET Core retains every derived factory until its parent is disposed.
                // Disposing this case-owned root bounds all pod hosts to one expanded case.
                await rootFactory.DisposeAsync();
                rootFactoryDisposed = true;
                _rootFactory = null;
                _clusterTask = null;
            }
            catch (Exception exception)
            {
                (failures ??= []).Add(exception);
                rootFactoryDisposed = rootFactory.BaseFactoryDisposed;
            }

            if (rootFactoryDisposed)
            {
                _rootFactory = null;
                _clusterTask = null;
            }
        }

        if (rootFactoryDisposed && catalog is not null)
        {
            try
            {
                await catalog.DisposeAsync();
                _catalog = null;
            }
            catch (Exception exception)
            {
                (failures ??= []).Add(exception);
            }
        }

        if (rootFactoryDisposed && !string.IsNullOrWhiteSpace(TestRoot))
        {
            try
            {
                if (Directory.Exists(TestRoot))
                    Directory.Delete(TestRoot, recursive: true);
            }
            catch (Exception exception)
            {
                (failures ??= []).Add(exception);
            }
        }

        if (failures is { Count: > 0 })
            throw new AggregateException($"Failed to dispose component resources for catalog '{CatalogName}'.", failures);
    }

    private static async Task<Cluster> StartClusterCoreAsync(
        ComponentTestWebApplicationFactory rootFactory,
        ComponentTestHostOptions hostOptions,
        TestContext testContext)
    {
        var primary = await WorkflowServer.CreateAsync(rootFactory, hostOptions, testContext);
        return new Cluster(
            primary,
            () => WorkflowServer.CreateAsync(rootFactory, hostOptions, testContext));
    }

}

/// <summary>
/// Provisions one case-owned catalog from inside the native TUnit.AspNetCore server-start gate.
/// Every pod for the case shares the same memoized provisioning task.
/// </summary>
internal sealed class ComponentTestCatalog(
    string catalogName,
    string connectionString,
    string masterConnectionString) : IAsyncDisposable
{
    private readonly Lock _lifecycleLock = new();
    private Task? _provisionTask;
    private Task? _migrationTask;
    private Task? _disposeTask;
    private int _cleanupRequired;

    public Task ProvisionAsync(CancellationToken cancellationToken)
    {
        lock (_lifecycleLock)
        {
            ObjectDisposedException.ThrowIf(_disposeTask is not null, this);
            return _provisionTask ??= ProvisionCoreAsync(cancellationToken);
        }
    }

    public ValueTask DisposeAsync()
    {
        lock (_lifecycleLock)
        {
            _disposeTask ??= DisposeCoreAsync(_provisionTask);
            return new ValueTask(_disposeTask);
        }
    }

    public Task MigrateAsync(Func<CancellationToken, Task> migrateAsync, CancellationToken cancellationToken)
    {
        lock (_lifecycleLock)
        {
            ObjectDisposedException.ThrowIf(_disposeTask is not null, this);
            return _migrationTask ??= MigrateCoreAsync(migrateAsync, cancellationToken);
        }
    }

    private async Task ProvisionCoreAsync(CancellationToken cancellationToken)
    {
        // Claim cleanup ownership before CREATE: a transport or cancellation failure can be
        // ambiguous even when SQL Server has already committed the database creation.
        Volatile.Write(ref _cleanupRequired, 1);

        await using var connection = new SqlConnection(masterConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE {QuoteIdentifier(catalogName)}";
        command.CommandTimeout = 30;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task MigrateCoreAsync(Func<CancellationToken, Task> migrateAsync, CancellationToken cancellationToken)
    {
        await ProvisionAsync(cancellationToken);
        await migrateAsync(cancellationToken);
    }

    private async Task DisposeCoreAsync(Task? provisionTask)
    {
        var migrationTask = _migrationTask;
        if (migrationTask is not null)
        {
            try
            {
                await migrationTask;
            }
            catch
            {
                // The host startup path owns the migration failure. Catalog cleanup still runs.
            }
        }

        if (provisionTask is not null)
        {
            try
            {
                await provisionTask;
            }
            catch
            {
                // The host startup path owns the provisioning failure. Cleanup must still probe
                // for a catalog because SQL Server may have committed CREATE before the failure.
            }
        }

        if (Volatile.Read(ref _cleanupRequired) == 0)
            return;

        // Every catalog has a distinct connection string and pool. Clear only this case's pool
        // after all of its hosts are down so DROP cannot disturb a concurrently running sibling.
        using (var pooledConnection = new SqlConnection(connectionString))
            SqlConnection.ClearPool(pooledConnection);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await using var connection = new SqlConnection(masterConnectionString);
        await connection.OpenAsync(timeout.Token);
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            IF DB_ID(@catalogName) IS NOT NULL
            BEGIN
                ALTER DATABASE {QuoteIdentifier(catalogName)} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE {QuoteIdentifier(catalogName)};
            END
            """;
        command.CommandTimeout = 30;
        command.Parameters.AddWithValue("@catalogName", catalogName);
        await command.ExecuteNonQueryAsync(timeout.Token);
        Volatile.Write(ref _cleanupRequired, 0);
    }

    private static string QuoteIdentifier(string identifier) => $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]";
}
