using Elsa.Connections.Credentials.Persistence.EFCore;
using Elsa.Connections.Credentials.Persistence.EFCore.PostgreSql;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Secrets.Models;
using Elsa.Secrets.Persistence.EFCore;
using Elsa.Secrets.Persistence.EFCore.PostgreSql;
using Elsa.Slack.SocketMode;
using Elsa.Workflows.Admission;
using Elsa.Workflows.Admission.Persistence.EFCore;
using Elsa.Workflows.Admission.Persistence.EFCore.PostgreSql.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Slack.Tests.Credentials;

public sealed class SlackSocketHostLayoutTests
{
    [Theory]
    [InlineData("admission")]
    [InlineData("connections")]
    [InlineData("secrets")]
    public void EffectiveSelectedHistoryAndAllTableSchemasAreRequired(string kind)
    {
        using var services = new ServiceCollection().AddPostgreSqlEntityModelCreatingHandlers().BuildServiceProvider();
        using var valid = CreateContext(kind, services);
        SlackSocketModeHostValidator.DemandDatabaseLayout(valid);
        using var wrongHistory = CreateContext(kind, services, wrongHistory: true);
        Assert.Throws<InvalidOperationException>(() => SlackSocketModeHostValidator.DemandDatabaseLayout(wrongHistory));
        using var wrongSchema = CreateContext(kind, services, wrongSchema: true);
        Assert.Throws<InvalidOperationException>(() => SlackSocketModeHostValidator.DemandDatabaseLayout(wrongSchema));
        using var wrongTable = CreateContext(kind, services, wrongTable: true);
        Assert.Throws<InvalidOperationException>(() => SlackSocketModeHostValidator.DemandDatabaseLayout(wrongTable));
    }

    [Fact]
    public void SelectedConnectionsAndSecretsIntentionallyShareExistingHistory()
    {
        using var services = new ServiceCollection().AddPostgreSqlEntityModelCreatingHandlers().BuildServiceProvider();
        using var connections = CreateContext("connections", services);
        using var secrets = CreateContext("secrets", services);
        SlackSocketModeHostValidator.DemandDatabaseLayout(connections);
        SlackSocketModeHostValidator.DemandDatabaseLayout(secrets);
        Assert.Equal("__EFMigrationsHistory", ElsaDbContextBase.MigrationsHistoryTable);
    }

    private static ElsaDbContextBase CreateContext(string kind, IServiceProvider services,
        bool wrongHistory = false, bool wrongSchema = false, bool wrongTable = false)
    {
        var admission = kind == "admission";
        var options = new ElsaDbContextOptions
        {
            SchemaName = wrongSchema ? "Other" : "Elsa",
            MigrationsHistoryTableName = wrongHistory
                ? admission ? ElsaDbContextBase.MigrationsHistoryTable : "__AdmissionMigrationsHistory"
                : admission ? "__AdmissionMigrationsHistory" : ElsaDbContextBase.MigrationsHistoryTable
        };
        if (wrongTable)
        {
            options.ConfigureModel<AdmissionElsaDbContext>(builder => builder.Entity<AdmissionRecord>().ToTable("Admissions", "Other"));
            options.ConfigureModel<ConnectionsElsaDbContext>(builder => builder.Entity<Elsa.Connections.Models.IntegrationConnection>().ToTable("Connections", "Other"));
            options.ConfigureModel<SecretsElsaDbContext>(builder => builder.Entity<Secret>().ToTable("Secrets", "Other"));
        }
        // Metadata-only contexts, no connection opening/migration. A fresh internal EF provider
        // per variant prevents model caching from hiding the deliberately changed table schema.
        var connection = "Host=localhost;Database=socket_layout_metadata;Username=metadata;Password=synthetic";
        switch (kind)
        {
            case "admission":
                var admissionBuilder = new DbContextOptionsBuilder<AdmissionElsaDbContext>();
                admissionBuilder.EnableServiceProviderCaching(false).UseElsaPostgreSql(typeof(PostgreSqlAdmissionPersistenceExtensions).Assembly, connection, options);
                return new AdmissionElsaDbContext(admissionBuilder.Options, services);
            case "connections":
                var connectionsBuilder = new DbContextOptionsBuilder<ConnectionsElsaDbContext>();
                connectionsBuilder.EnableServiceProviderCaching(false).UseElsaPostgreSql(typeof(ConnectionsDbContextFactory).Assembly, connection, options);
                return new ConnectionsElsaDbContext(connectionsBuilder.Options, services);
            case "secrets":
                var secretsBuilder = new DbContextOptionsBuilder<SecretsElsaDbContext>();
                secretsBuilder.EnableServiceProviderCaching(false).UseElsaPostgreSql(typeof(SecretsDbContextFactory).Assembly, connection, options);
                return new SecretsElsaDbContext(secretsBuilder.Options, services);
            default:
                throw new ArgumentException("Unknown metadata context.", nameof(kind));
        }
    }
}
