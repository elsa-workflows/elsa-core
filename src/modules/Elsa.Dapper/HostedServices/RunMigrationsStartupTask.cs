using Elsa.Common;
using FluentMigrator.Runner;
using JetBrains.Annotations;

namespace Elsa.Dapper.HostedServices;

/// <summary>
/// Runs database migrations on startup.
/// </summary>
[UsedImplicitly]
public class RunMigrationsStartupTask(IMigrationRunner migrationRunner) : IStartupTask
{
    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        migrationRunner.MigrateUp();
        return Task.CompletedTask;
    }
}