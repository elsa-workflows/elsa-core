using Elsa.Persistence.Dapper.Contracts;
using Elsa.Persistence.Dapper.HostedServices;
using Elsa.Persistence.Dapper.Migrations.Management;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.Dapper.Features;

/// <summary>
/// Configures migrations.
/// </summary>
public class DapperMigrationsFeature(IModule module) : FeatureBase(module)
{
    /// <summary>
    /// Gets or sets a delegate used to configure the <see cref="IMigrationRunnerBuilder"/> for Dapper migrations.
    /// </summary>
    /// <remarks>
    /// The delegate must configure a database provider on the runner (for example by calling
    /// <c>AddSqlServer()</c> or <c>AddSQLite()</c>) in addition to any other settings.
    /// This replaces the removed <c>UseSqlServer()</c> and <c>UseSqlite()</c> convenience methods;
    /// consumers migrating from those APIs should move their provider configuration into this delegate.
    /// </remarks>
    public Action<IMigrationRunnerBuilder> ConfigureRunner { get; set; } = runner => runner
        .WithGlobalConnectionString(sp => sp.GetRequiredService<IDbConnectionProvider>().GetConnectionString())
        .WithMigrationsIn(typeof(Initial).Assembly);

    /// <inheritdoc />
    public override void Configure()
    {
        Services.AddFluentMigratorCore();
        Services.ConfigureRunner(ConfigureRunner);
        Services.AddStartupTask<RunMigrationsStartupTask>();
    }
}