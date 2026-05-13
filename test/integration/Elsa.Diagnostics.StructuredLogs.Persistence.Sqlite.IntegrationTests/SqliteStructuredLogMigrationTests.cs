using Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.Options;
using Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests;

public class SqliteStructuredLogMigrationTests
{
    [Fact]
    public async Task MigrateAsync_CreatesStructuredLogTableInEmptyDatabase()
    {
        await using var host = new SqliteStructuredLogTestHost(migrate: false);

        await host.Migrator.MigrateAsync();

        Assert.True(await host.TableExistsAsync("StructuredLogEvents"));
    }

    [Fact]
    public async Task Startup_DoesNotRunMigrations_WhenOptedOut()
    {
        await using var host = new SqliteStructuredLogTestHost(options => options.RunMigrationsOnStartup = false, migrate: false);
        var startup = host.Services.GetServices<IHostedService>().OfType<SqliteStructuredLogStartupService>().Single();

        await startup.StartAsync(CancellationToken.None);

        Assert.False(await host.TableExistsAsync("StructuredLogEvents"));
    }
}
