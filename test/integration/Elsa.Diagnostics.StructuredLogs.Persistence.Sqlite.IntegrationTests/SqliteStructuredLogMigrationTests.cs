using Elsa.Common;
using Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.Options;
using Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.Services;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests;

public class SqliteStructuredLogMigrationTests
{
    [Test]
    public async Task MigrateAsync_CreatesStructuredLogTableInEmptyDatabase()
    {
        await using var host = new SqliteStructuredLogTestHost(migrate: false);

        await host.Migrator.MigrateAsync();

        await Assert.That(await host.TableExistsAsync("StructuredLogEvents")).IsTrue();
    }

    [Test]
    public async Task Startup_DoesNotRunMigrations_WhenOptedOut()
    {
        await using var host = new SqliteStructuredLogTestHost(options => options.RunMigrationsOnStartup = false, migrate: false);
        var startup = host.Services.GetServices<IHostedService>().OfType<SqliteStructuredLogStartupService>().Single();

        await startup.StartAsync(CancellationToken.None);

        await Assert.That(await host.TableExistsAsync("StructuredLogEvents")).IsFalse();
    }

    [Test]
    public async Task StartupTask_RunsMigrations_WhenHostedServicesAreNotStarted()
    {
        await using var host = new SqliteStructuredLogTestHost(migrate: false);
        using var scope = host.Services.CreateScope();
        var startup = scope.ServiceProvider.GetServices<IStartupTask>().OfType<SqliteStructuredLogStartupService>().Single();

        await startup.ExecuteAsync(CancellationToken.None);

        await Assert.That(await host.TableExistsAsync("StructuredLogEvents")).IsTrue();
    }

    [Test]
    public async Task StartupTask_UsesSameInstance_AsHostedService()
    {
        await using var host = new SqliteStructuredLogTestHost(migrate: false);
        using var scope = host.Services.CreateScope();
        var hostedService = host.Services.GetServices<IHostedService>().OfType<SqliteStructuredLogStartupService>().Single();
        var startupTask = scope.ServiceProvider.GetServices<IStartupTask>().OfType<SqliteStructuredLogStartupService>().Single();

        await Assert.That(startupTask).IsSameReferenceAs(hostedService);
    }

    [Test]
    public async Task HostedServices_StartMigrationBeforeWriteBuffer()
    {
        await using var host = new SqliteStructuredLogTestHost(migrate: false);

        var hostedServiceTypes = host.Services.GetServices<IHostedService>().Select(x => x.GetType()).ToList();

        await Assert.That(
                hostedServiceTypes.IndexOf(typeof(SqliteStructuredLogStartupService)) < hostedServiceTypes.IndexOf(typeof(StructuredLogWriteBuffer)))
            .IsTrue()
            .Because("SQLite migrations must run before the durable write buffer starts flushing queued logs.");
    }
}
