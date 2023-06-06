using Elsa.Dapper.Contracts;
using Elsa.Dapper.HostedServices;
using Elsa.Dapper.Migrations.Management;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Dapper.Features;

/// <summary>
/// Configures migrations.
/// </summary>
public class DapperMigrationsFeature : FeatureBase
{
    /// <inheritdoc />
    public DapperMigrationsFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// Gets or sets a delegate to configure migrations.
    /// </summary>
    public Action<IMigrationRunnerBuilder> ConfigureRunner { get; set; } = runner => runner
        .AddSQLite()
        .WithGlobalConnectionString(sp => sp.GetRequiredService<IDbConnectionProvider>().GetConnectionString())
        .WithMigrationsIn(typeof(Initial).Assembly);

    /// <inheritdoc />
    public override void Configure()
    {
        Services.AddFluentMigratorCore();
        Services.ConfigureRunner(ConfigureRunner);
    }

    /// <inheritdoc />
    public override void ConfigureHostedServices()
    {
        ConfigureHostedService<RunMigrationsHostedService>(-1);
    }
}