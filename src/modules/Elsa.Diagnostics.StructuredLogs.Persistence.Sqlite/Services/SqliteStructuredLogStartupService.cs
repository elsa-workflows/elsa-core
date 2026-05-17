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
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await ExecuteAsync(cancellationToken);
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (options.Value.RunMigrationsOnStartup)
            await schemaMigrator.MigrateAsync(cancellationToken);

        if (options.Value.Relational.Retention.CleanupOnStartup)
            await retentionService.CleanupAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
