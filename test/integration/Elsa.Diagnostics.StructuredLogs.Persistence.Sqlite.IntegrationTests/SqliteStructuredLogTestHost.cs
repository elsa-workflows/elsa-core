using Elsa.Diagnostics.StructuredLogs.Contracts;
using Elsa.Diagnostics.StructuredLogs.Extensions;
using Elsa.Diagnostics.StructuredLogs.Models;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Contracts;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Services;
using Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.Extensions;
using Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.Options;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests;

internal sealed class SqliteStructuredLogTestHost : IAsyncDisposable
{
    private readonly string _directory;

    public SqliteStructuredLogTestHost(Action<SqliteStructuredLogOptions>? configure = null, bool migrate = true)
    {
        _directory = Path.Join(Path.GetTempPath(), $"elsa-structured-logs-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
        DatabasePath = Path.Join(_directory, "structured-logs.db");
        ConnectionString = $"Data Source={DatabasePath}";

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddStructuredLogsServices();
        services.AddSqliteStructuredLogPersistence(options =>
        {
            options.ConnectionString = ConnectionString;
            configure?.Invoke(options);
        });

        Services = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        if (migrate)
            Migrator.MigrateAsync().AsTask().GetAwaiter().GetResult();
    }

    public string DatabasePath { get; }
    public string ConnectionString { get; }
    public ServiceProvider Services { get; }
    public IStructuredLogStore Store => Services.GetRequiredService<IStructuredLogStore>();
    public IStructuredLogProvider Provider => Services.GetRequiredService<IStructuredLogProvider>();
    public IStructuredLogWriteBuffer Buffer => Services.GetRequiredService<IStructuredLogWriteBuffer>();
    public IStructuredLogSchemaMigrator Migrator => Services.GetRequiredService<IStructuredLogSchemaMigrator>();
    public StructuredLogRetentionService Retention => Services.GetRequiredService<StructuredLogRetentionService>();

    public async ValueTask WriteAsync(params StructuredLogEvent[] events)
    {
        await Buffer.WriteManyAsync(events);
        await Buffer.FlushAsync();
    }

    public async ValueTask StartHostedServicesAsync()
    {
        foreach (var hostedService in Services.GetServices<IHostedService>())
            await hostedService.StartAsync(CancellationToken.None);
    }

    public async ValueTask StopHostedServicesAsync()
    {
        foreach (var hostedService in Services.GetServices<IHostedService>().Reverse())
            await hostedService.StopAsync(CancellationToken.None);
    }

    public async ValueTask<int> CountRowsAsync(string tableName)
    {
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {tableName}";
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    public async ValueTask<bool> TableExistsAsync(string tableName)
    {
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = @Name";
        command.Parameters.AddWithValue("@Name", tableName);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
    }

    public async ValueTask<IReadOnlyCollection<string>> ReadRawTimestampsAsync()
    {
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Timestamp FROM StructuredLogEvents ORDER BY Timestamp";
        await using var reader = await command.ExecuteReaderAsync();
        var values = new List<string>();

        while (await reader.ReadAsync())
            values.Add(reader.GetString(0));

        return values;
    }

    public async ValueTask DisposeAsync()
    {
        await Services.DisposeAsync();

        if (Directory.Exists(_directory))
            Directory.Delete(_directory, true);
    }
}

internal static class StructuredLogTestEvents
{
    public static StructuredLogEvent Create(
        string id,
        DateTimeOffset? timestamp = null,
        StructuredLogLevel level = StructuredLogLevel.Information,
        string category = "Elsa.Tests",
        string sourceId = "source-a",
        string? workflowDefinitionId = null,
        string? workflowInstanceId = null,
        string? correlationId = null,
        string? traceId = null,
        string message = "Test event")
    {
        var now = timestamp ?? DateTimeOffset.UtcNow;
        return new()
        {
            Id = id,
            Sequence = Math.Abs(id.GetHashCode(StringComparison.Ordinal)),
            Timestamp = now,
            ReceivedAt = now,
            Level = level,
            Category = category,
            EventId = 1,
            Message = message,
            MessageTemplate = "Test event",
            SourceId = sourceId,
            WorkflowDefinitionId = workflowDefinitionId,
            WorkflowInstanceId = workflowInstanceId,
            CorrelationId = correlationId,
            TraceId = traceId,
            Properties = new Dictionary<string, string?> { ["Property"] = message }
        };
    }
}
