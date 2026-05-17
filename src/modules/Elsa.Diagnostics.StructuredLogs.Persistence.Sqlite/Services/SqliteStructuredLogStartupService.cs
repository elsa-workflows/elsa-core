using Elsa.Common;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Contracts;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Services;
using Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.Services;

public class SqliteStructuredLogStartupService(
    IStructuredLogSchemaMigrator schemaMigrator,
    StructuredLogRetentionService retentionService,
    IOptions<SqliteStructuredLogOptions> options) : IHostedService, IStartupTask
{
    private readonly SemaphoreSlim _startupLock = new(1, 1);
    private bool _executed;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await ExecuteAsync(cancellationToken);
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (_executed)
            return;

        await _startupLock.WaitAsync(cancellationToken);
        try
        {
            if (_executed)
                return;

            if (options.Value.RunMigrationsOnStartup)
                await schemaMigrator.MigrateAsync(cancellationToken);

            if (options.Value.Relational.Retention.CleanupOnStartup)
                await retentionService.CleanupAsync(cancellationToken);

            _executed = true;
        }
        finally
        {
            _startupLock.Release();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
