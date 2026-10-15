using System.Data;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Labels;
using Elsa.Persistence.EFCore.SqlServer;
using Elsa.Secrets.Persistence.EFCore;
using Elsa.Secrets.Persistence.EFCore.SqlServer;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Elsa.Workflows.ComponentTests.Fixtures;

namespace Elsa.Workflows.ComponentTests.Scenarios.Persistence;

[Collection(nameof(AppCollection))]
public class SqlServerUniquenessMigrationsTests(App app)
{
    private const string SecretTenancyMigration = "20260825230253_SecretTenancy";
    private const string SecretUniquenessMigration = "20260914120000_SecretDefaultTenantUniqueness";
    private const string LabelsBaselineMigration = "20250222190946_V3_4";
    private const string LabelsUniquenessMigration = "20260913133317_PerTenantLabelUniqueness";

    [Fact]
    public Task SecretDefaultTenantUniqueness_AppliesAndStampsNullTenantIds()
    {
        return WithSecretsDatabase(async context =>
        {
            await MigrateToAsync(context, SecretTenancyMigration);
            await context.Database.ExecuteSqlRawAsync("""
                INSERT INTO [Elsa].[Secrets]
                    ([Id], [Name], [NormalizedName], [DisplayName], [TypeName], [StoreName], [Status], [CreatedAt], [Tags], [Versions], [TenantId])
                VALUES
                    (N'secret-1', N'one', N'ONE', N'one', N'type', N'store', N'Active', SYSUTCDATETIME(), N'[]', N'[]', NULL),
                    (N'secret-2', N'two', N'TWO', N'two', N'type', N'store', N'Active', SYSUTCDATETIME(), N'[]', N'[]', NULL);
                """);

            await MigrateToAsync(context, SecretUniquenessMigration);

            Assert.Equal(2, await ScalarAsync(context, "SELECT COUNT(*) FROM [Elsa].[Secrets] WHERE [TenantId] = N''"));
            Assert.Equal(0, await ScalarAsync(context, "SELECT COUNT(*) FROM [Elsa].[Secrets] WHERE [TenantId] IS NULL"));
            var duplicate = await Assert.ThrowsAsync<SqlException>(() => context.Database.ExecuteSqlRawAsync("""
                INSERT INTO [Elsa].[Secrets]
                    ([Id], [Name], [NormalizedName], [DisplayName], [TypeName], [StoreName], [Status], [CreatedAt], [Tags], [Versions], [TenantId])
                VALUES (N'secret-duplicate', N'duplicate', N'ONE', N'duplicate', N'type', N'store', N'Active', SYSUTCDATETIME(), N'[]', N'[]', N'');
                """));
            Assert.True(duplicate.Number is 2601 or 2627);
            await context.Database.ExecuteSqlRawAsync("""
                INSERT INTO [Elsa].[Secrets]
                    ([Id], [Name], [NormalizedName], [DisplayName], [TypeName], [StoreName], [Status], [CreatedAt], [Tags], [Versions], [TenantId])
                VALUES (N'secret-other-tenant', N'other tenant', N'ONE', N'other tenant', N'type', N'store', N'Active', SYSUTCDATETIME(), N'[]', N'[]', N'tenant-b');
                """);
        });
    }

    [Fact]
    public Task SecretDefaultTenantUniqueness_ReportsDuplicateErrorAndRollsBackTenantStamp()
    {
        return WithSecretsDatabase(async context =>
        {
            await MigrateToAsync(context, SecretTenancyMigration);
            await context.Database.ExecuteSqlRawAsync("""
                INSERT INTO [Elsa].[Secrets]
                    ([Id], [Name], [NormalizedName], [DisplayName], [TypeName], [StoreName], [Status], [CreatedAt], [Tags], [Versions], [TenantId])
                VALUES
                    (N'duplicate-1', N'duplicate', N'DUPLICATE', N'duplicate', N'type', N'store', N'Active', SYSUTCDATETIME(), N'[]', N'[]', NULL),
                    (N'duplicate-2', N'duplicate', N'DUPLICATE', N'duplicate', N'type', N'store', N'Active', SYSUTCDATETIME(), N'[]', N'[]', NULL);
                """);

            var exception = await Assert.ThrowsAsync<SqlException>(() => MigrateToAsync(context, SecretUniquenessMigration));

            Assert.Equal(50001, exception.Number);
            Assert.Equal(2, await ScalarAsync(context, "SELECT COUNT(*) FROM [Elsa].[Secrets] WHERE [TenantId] IS NULL AND [NormalizedName] = N'DUPLICATE'"));
            Assert.Equal(0, await ScalarAsync(context, "SELECT COUNT(*) FROM [Elsa].[Secrets] WHERE [TenantId] = N''"));
            Assert.Equal(0, await MigrationAppliedAsync(context, SecretUniquenessMigration));
        });
    }

    [Fact]
    public Task PerTenantLabelUniqueness_AppliesAndStampsNullTenantIds()
    {
        return WithLabelsDatabase(async context =>
        {
            await MigrateToAsync(context, LabelsBaselineMigration);
            await context.Database.ExecuteSqlRawAsync("""
                INSERT INTO [Elsa].[Labels] ([Id], [Name], [NormalizedName], [Description], [Color], [TenantId])
                VALUES (N'label-1', N'one', N'ONE', NULL, NULL, NULL),
                       (N'label-2', N'two', N'TWO', NULL, NULL, NULL);
                """);

            await MigrateToAsync(context, LabelsUniquenessMigration);

            Assert.Equal(2, await ScalarAsync(context, "SELECT COUNT(*) FROM [Elsa].[Labels] WHERE [TenantId] = N''"));
            Assert.Equal(0, await ScalarAsync(context, "SELECT COUNT(*) FROM [Elsa].[Labels] WHERE [TenantId] IS NULL"));
            var duplicate = await Assert.ThrowsAsync<SqlException>(() => context.Database.ExecuteSqlRawAsync("""
                INSERT INTO [Elsa].[Labels] ([Id], [Name], [NormalizedName], [Description], [Color], [TenantId])
                VALUES (N'label-duplicate', N'duplicate', N'ONE', NULL, NULL, N'');
                """));
            Assert.True(duplicate.Number is 2601 or 2627);
            await context.Database.ExecuteSqlRawAsync("""
                INSERT INTO [Elsa].[Labels] ([Id], [Name], [NormalizedName], [Description], [Color], [TenantId])
                VALUES (N'label-other-tenant', N'other tenant', N'ONE', NULL, NULL, N'tenant-b');
                """);
        });
    }

    [Fact]
    public Task PerTenantLabelUniqueness_ReportsDuplicateErrorAndRollsBackTenantStamp()
    {
        return WithLabelsDatabase(async context =>
        {
            await MigrateToAsync(context, LabelsBaselineMigration);
            await context.Database.ExecuteSqlRawAsync("""
                INSERT INTO [Elsa].[Labels] ([Id], [Name], [NormalizedName], [Description], [Color], [TenantId])
                VALUES (N'duplicate-1', N'duplicate', N'DUPLICATE', NULL, NULL, NULL),
                       (N'duplicate-2', N'duplicate', N'DUPLICATE', NULL, NULL, NULL);
                """);

            var exception = await Assert.ThrowsAsync<SqlException>(() => MigrateToAsync(context, LabelsUniquenessMigration));

            Assert.Equal(50001, exception.Number);
            Assert.Equal(2, await ScalarAsync(context, "SELECT COUNT(*) FROM [Elsa].[Labels] WHERE [TenantId] IS NULL AND [NormalizedName] = N'DUPLICATE'"));
            Assert.Equal(0, await ScalarAsync(context, "SELECT COUNT(*) FROM [Elsa].[Labels] WHERE [TenantId] = N''"));
            Assert.Equal(0, await MigrationAppliedAsync(context, LabelsUniquenessMigration));
        });
    }

    [Fact]
    public Task PerTenantLabelUniqueness_ReportsOverLengthErrorWithoutNarrowingOrChangingRows()
    {
        return WithLabelsDatabase(async context =>
        {
            await MigrateToAsync(context, LabelsBaselineMigration);
            await context.Database.ExecuteSqlRawAsync("""
                INSERT INTO [Elsa].[Labels] ([Id], [Name], [NormalizedName], [Description], [Color], [TenantId])
                VALUES (N'overlength', REPLICATE(N'x', 256), REPLICATE(N'X', 256), NULL, NULL, N'tenant');
                """);

            var exception = await Assert.ThrowsAsync<SqlException>(() => MigrateToAsync(context, LabelsUniquenessMigration));

            Assert.Equal(50002, exception.Number);
            Assert.Equal(1, await ScalarAsync(context, "SELECT COUNT(*) FROM [Elsa].[Labels] WHERE [Id] = N'overlength' AND LEN([Name]) = 256 AND LEN([NormalizedName]) = 256"));
            Assert.Equal(0, await MigrationAppliedAsync(context, LabelsUniquenessMigration));
        });
    }

    private async Task WithSecretsDatabase(Func<SecretsElsaDbContext, Task> action)
    {
        await WithDatabaseAsync(async connectionString =>
        {
            var optionsBuilder = new DbContextOptionsBuilder<SecretsElsaDbContext>();
            optionsBuilder.UseElsaSqlServer(typeof(SecretsDbContextFactory).Assembly, connectionString);
            using var context = new SecretsElsaDbContext(optionsBuilder.Options, new ServiceCollection().BuildServiceProvider());
            await action(context);
        });
    }

    private async Task WithLabelsDatabase(Func<LabelsElsaDbContext, Task> action)
    {
        await WithDatabaseAsync(async connectionString =>
        {
            var optionsBuilder = new DbContextOptionsBuilder<LabelsElsaDbContext>();
            optionsBuilder.UseElsaSqlServer(typeof(LabelsDbContextFactory).Assembly, connectionString);
            using var context = new LabelsElsaDbContext(optionsBuilder.Options, new ServiceCollection().BuildServiceProvider());
            await action(context);
        });
    }

    private async Task WithDatabaseAsync(Func<string, Task> action)
    {
        var databaseName = $"ElsaMigration{Guid.NewGuid():N}";
        var masterConnectionString = new SqlConnectionStringBuilder(app.Infrastructure.DbContainer.GetConnectionString())
        {
            InitialCatalog = "master"
        }.ConnectionString;

        await using (var masterConnection = new SqlConnection(masterConnectionString))
        {
            await masterConnection.OpenAsync();
            await using var command = masterConnection.CreateCommand();
            command.CommandText = $"CREATE DATABASE [{databaseName}]";
            await command.ExecuteNonQueryAsync();
        }

        var databaseConnectionString = new SqlConnectionStringBuilder(app.Infrastructure.DbContainer.GetConnectionString())
        {
            InitialCatalog = databaseName
        }.ConnectionString;

        try
        {
            await action(databaseConnectionString);
        }
        finally
        {
            await using var masterConnection = new SqlConnection(masterConnectionString);
            await masterConnection.OpenAsync();
            await using var command = masterConnection.CreateCommand();
            command.CommandText = $"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{databaseName}]";
            await command.ExecuteNonQueryAsync();
        }
    }

    private static Task MigrateToAsync(DbContext context, string migrationId) =>
        context.GetService<IMigrator>().MigrateAsync(migrationId);

    private static async Task<int> ScalarAsync(DbContext context, string sql)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
            await connection.OpenAsync();

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }

    private static async Task<int> MigrationAppliedAsync(DbContext context, string migrationId)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
            await connection.OpenAsync();

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM [Elsa].[__EFMigrationsHistory] WHERE [MigrationId] = @migrationId";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@migrationId";
            parameter.Value = migrationId;
            command.Parameters.Add(parameter);
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }
}
