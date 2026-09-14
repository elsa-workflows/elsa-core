using Elsa.Expressions.Helpers;
using Elsa.Workflows.ComponentTests.Services;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using TUnit.Core.Interfaces;

namespace Elsa.Workflows.ComponentTests.Fixtures;

/// <summary>
/// Owns the SQL Server container and migrated database template shared by the native TUnit test session.
/// The session-shared <see cref="App"/> restores that immutable template exactly once.
/// </summary>
public sealed class Infrastructure : IAsyncInitializer, IAsyncDisposable
{
    private const string TemplateCatalogName = "elsa_component_template";
    private const string TemplateBackupPath = "/var/opt/mssql/data/elsa_component_template.bak";
    private MsSqlContainer? _dbContainer;
    private DatabaseTemplate? _databaseTemplate;
    private bool _strictModeCaptured;
    private bool _originalStrictMode;
    private int _disposed;

    public MsSqlContainer DbContainer => _dbContainer
        ?? throw new InvalidOperationException("The component-test SQL Server infrastructure has not been initialized.");

    public async Task InitializeAsync()
    {
        var testContext = TestContext.Current
            ?? throw new InvalidOperationException("A current TUnit test context is required to initialize component infrastructure.");

        // Building the Testcontainers object performs Docker endpoint discovery and can throw.
        // Do that before mutating the process-wide parity setting.
        var dbContainer = new MsSqlBuilder().Build();
        _dbContainer = dbContainer;
        _originalStrictMode = ObjectConverter.StrictMode;
        _strictModeCaptured = true;
        ObjectConverter.StrictMode = true;

        try
        {
            await dbContainer.StartAsync();
            _databaseTemplate = await CreateDatabaseTemplateAsync(testContext);
        }
        catch (Exception initializationFailure)
        {
            Exception? cleanupFailure = null;
            try
            {
                await dbContainer.DisposeAsync();
            }
            catch (Exception exception)
            {
                cleanupFailure = exception;
            }
            finally
            {
                _dbContainer = null;
                _databaseTemplate = null;
                RestoreStrictMode();
            }

            if (cleanupFailure is not null)
                throw new AggregateException("SQL Server infrastructure initialization and cleanup both failed.", initializationFailure, cleanupFailure);

            throw;
        }
    }

    internal async Task RestoreDatabaseAsync(string catalogName, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        var template = _databaseTemplate
            ?? throw new InvalidOperationException("The component database template has not been initialized.");
        var physicalName = $"elsa_component_{Guid.NewGuid():N}";
        var moveClauses = template.Files.Select(file =>
        {
            var extension = Path.GetExtension(file.PhysicalName);
            if (string.IsNullOrWhiteSpace(extension))
                extension = file.Type == "L" ? ".ldf" : ".mdf";
            var targetPath = $"/var/opt/mssql/data/{physicalName}_{file.FileId}{extension}";
            return $"MOVE N'{EscapeSqlLiteral(file.LogicalName)}' TO N'{EscapeSqlLiteral(targetPath)}'";
        });
        await using var connection = new SqlConnection(GetMasterConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            RESTORE DATABASE {QuoteIdentifier(catalogName)}
            FROM DISK = N'{EscapeSqlLiteral(template.BackupPath)}'
            WITH {string.Join(",\n     ", moveClauses)},
                 RECOVERY,
                 CHECKSUM,
                 NEW_BROKER;
            """;
        command.CommandTimeout = 120;
        // Once RESTORE starts, let SQL Server finish atomically instead of leaving a cancelled
        // test database in RESTORING state. CommandTimeout remains the hard upper bound.
        await command.ExecuteNonQueryAsync(CancellationToken.None);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        Exception? failure = null;
        var dbContainer = _dbContainer;

        try
        {
            if (dbContainer is not null)
                await dbContainer.DisposeAsync();
        }
        catch (Exception exception)
        {
            failure = exception;
        }
        finally
        {
            _dbContainer = null;
            _databaseTemplate = null;
            RestoreStrictMode();
        }

        if (failure is not null)
            throw new AggregateException("Failed to dispose the component-test session infrastructure.", failure);
    }

    private async Task<DatabaseTemplate> CreateDatabaseTemplateAsync(TestContext testContext)
    {
        var scenarioDirectory = Path.Combine(AppContext.BaseDirectory, "Scenarios");
        if (!Directory.Exists(scenarioDirectory))
            throw new DirectoryNotFoundException($"Component scenario directory '{scenarioDirectory}' was not found.");

        var testRoot = Path.Combine(Path.GetTempPath(), $"elsa_component_template_{Guid.NewGuid():N}");
        var lockDirectory = Path.Combine(testRoot, "locks");
        var httpFileCacheDirectory = Path.Combine(testRoot, "http-file-cache");
        Directory.CreateDirectory(lockDirectory);
        Directory.CreateDirectory(httpFileCacheDirectory);

        var masterConnectionString = GetMasterConnectionString();
        var templateConnectionString = GetConnectionString(TemplateCatalogName);
        var templateCatalog = new ComponentTestCatalog(
            TemplateCatalogName,
            templateConnectionString,
            masterConnectionString,
            cancellationToken => CreateEmptyDatabaseAsync(TemplateCatalogName, cancellationToken));
        var bootstrapTracker = new ComponentDatabaseBootstrapTracker();
        var hostOptions = new ComponentTestHostOptions(
            templateConnectionString,
            scenarioDirectory,
            lockDirectory,
            httpFileCacheDirectory,
            new WorkflowExecutionTracker(),
            templateCatalog,
            MigrateDatabase: true,
            DatabaseBootstrapOnly: true,
            DatabaseBootstrapTracker: bootstrapTracker);

        DatabaseTemplate? databaseTemplate = null;
        Exception? operationFailure = null;
        try
        {
            await StartAndStopTemplateHostAsync(hostOptions, testContext);
            if (bootstrapTracker.MigrationCount != 4)
                throw new InvalidOperationException($"Expected one migration for each of four component schemas, but observed {bootstrapTracker.MigrationCount}.");

            // The host has released every template connection. Clear its pool before BACKUP so
            // the session image is stable and subsequent test hosts can only use restored copies.
            using (var pooledConnection = new SqlConnection(templateConnectionString))
                SqlConnection.ClearPool(pooledConnection);

            databaseTemplate = await BackUpTemplateAsync(masterConnectionString, CancellationToken.None);
        }
        catch (Exception exception)
        {
            operationFailure = exception;
        }

        List<Exception>? cleanupFailures = null;
        try
        {
            await templateCatalog.DisposeAsync();
        }
        catch (Exception exception)
        {
            (cleanupFailures ??= []).Add(exception);
        }

        try
        {
            if (Directory.Exists(testRoot))
                Directory.Delete(testRoot, recursive: true);
        }
        catch (Exception exception)
        {
            (cleanupFailures ??= []).Add(exception);
        }

        if (operationFailure is not null && cleanupFailures is { Count: > 0 })
            throw new AggregateException("Database-template creation and cleanup both failed.", [operationFailure, .. cleanupFailures]);
        if (operationFailure is not null)
            throw new InvalidOperationException("Database-template creation failed.", operationFailure);
        if (cleanupFailures is { Count: > 0 })
            throw new AggregateException("Database-template cleanup failed.", cleanupFailures);

        return databaseTemplate
            ?? throw new InvalidOperationException("Database-template creation completed without producing backup metadata.");
    }

    private static async Task StartAndStopTemplateHostAsync(
        ComponentTestHostOptions hostOptions,
        TestContext testContext)
    {
        var rootFactory = new ComponentTestWebApplicationFactory();
        WorkflowServer? server = null;
        Exception? startupFailure = null;
        List<Exception>? cleanupFailures = null;

        try
        {
            server = await WorkflowServer.CreateAsync(rootFactory, hostOptions, testContext);
        }
        catch (Exception exception)
        {
            startupFailure = exception;
        }

        if (server is not null)
        {
            try
            {
                await server.DisposeAsync();
            }
            catch (Exception exception)
            {
                (cleanupFailures ??= []).Add(exception);
            }
        }

        try
        {
            await rootFactory.DisposeAsync();
        }
        catch (Exception exception)
        {
            (cleanupFailures ??= []).Add(exception);
        }

        if (startupFailure is not null && cleanupFailures is { Count: > 0 })
            throw new AggregateException("Database-template host startup and cleanup both failed.", [startupFailure, .. cleanupFailures]);
        if (startupFailure is not null)
            throw new InvalidOperationException("Database-template host startup failed.", startupFailure);
        if (cleanupFailures is { Count: > 0 })
            throw new AggregateException("Database-template host cleanup failed.", cleanupFailures);
    }

    private static async Task<DatabaseTemplate> BackUpTemplateAsync(
        string masterConnectionString,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(masterConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using (var backupCommand = connection.CreateCommand())
        {
            backupCommand.CommandText = $"""
                BACKUP DATABASE {QuoteIdentifier(TemplateCatalogName)}
                TO DISK = N'{EscapeSqlLiteral(TemplateBackupPath)}'
                WITH COPY_ONLY, INIT, CHECKSUM;
                """;
            backupCommand.CommandTimeout = 120;
            await backupCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var verifyCommand = connection.CreateCommand())
        {
            verifyCommand.CommandText = $"RESTORE VERIFYONLY FROM DISK = N'{EscapeSqlLiteral(TemplateBackupPath)}' WITH CHECKSUM;";
            verifyCommand.CommandTimeout = 120;
            await verifyCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var fileListCommand = connection.CreateCommand();
        fileListCommand.CommandText = $"RESTORE FILELISTONLY FROM DISK = N'{EscapeSqlLiteral(TemplateBackupPath)}';";
        fileListCommand.CommandTimeout = 30;
        await using var reader = await fileListCommand.ExecuteReaderAsync(cancellationToken);
        var logicalNameOrdinal = reader.GetOrdinal("LogicalName");
        var physicalNameOrdinal = reader.GetOrdinal("PhysicalName");
        var typeOrdinal = reader.GetOrdinal("Type");
        var fileIdOrdinal = reader.GetOrdinal("FileId");
        var files = new List<DatabaseFile>();

        while (await reader.ReadAsync(cancellationToken))
        {
            files.Add(new DatabaseFile(
                reader.GetString(logicalNameOrdinal),
                reader.GetString(physicalNameOrdinal),
                reader.GetString(typeOrdinal),
                Convert.ToInt32(reader.GetValue(fileIdOrdinal))));
        }

        if (files.Count == 0 || files.All(x => x.Type != "D") || files.All(x => x.Type != "L"))
            throw new InvalidOperationException("The component database template backup did not contain both data and log files.");

        return new DatabaseTemplate(TemplateBackupPath, files);
    }

    private async Task CreateEmptyDatabaseAsync(string catalogName, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(GetMasterConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE {QuoteIdentifier(catalogName)}";
        command.CommandTimeout = 30;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private string GetConnectionString(string catalogName) => new SqlConnectionStringBuilder(DbContainer.GetConnectionString())
    {
        InitialCatalog = catalogName,
        Pooling = true
    }.ConnectionString;

    private string GetMasterConnectionString() => new SqlConnectionStringBuilder(DbContainer.GetConnectionString())
    {
        InitialCatalog = "master",
        Pooling = false
    }.ConnectionString;

    private void RestoreStrictMode()
    {
        if (!_strictModeCaptured)
            return;

        ObjectConverter.StrictMode = _originalStrictMode;
        _strictModeCaptured = false;
    }

    private static string QuoteIdentifier(string identifier) => $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]";
    private static string EscapeSqlLiteral(string value) => value.Replace("'", "''", StringComparison.Ordinal);

    private sealed record DatabaseTemplate(
        string BackupPath,
        IReadOnlyList<DatabaseFile> Files);

    private sealed record DatabaseFile(string LogicalName, string PhysicalName, string Type, int FileId);
}
