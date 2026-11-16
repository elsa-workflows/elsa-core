using System.Data;
using System.Security.Cryptography;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Labels;
using Elsa.Secrets.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using Xunit.Abstractions;

namespace Elsa.Persistence.EFCore.MySql.UnitTests;

/// <summary>
/// Runs the MySQL per-tenant uniqueness migrations for issue #9114 against a real MySQL
/// server: the clean path must still succeed, and duplicates must fail at CreateIndex
/// with error 1062 (naming the duplicate value and the index) instead of the previous
/// preflight's ERROR 1295 from SIGNAL via PREPARE. No duplicate rows are deleted; a null
/// TenantId may be stamped to "" before CreateIndex fails.
///
/// These tests require <c>ELSA_MYSQL_CONNECTION_STRING</c> to point at a MySQL server the
/// caller controls, e.g.:
///
/// <code>
/// export MYSQL_ROOT_PASSWORD="$(openssl rand -base64 32)"
/// docker run --rm -p 3306:3306 -e MYSQL_ROOT_PASSWORD="$MYSQL_ROOT_PASSWORD" mysql:8.0
/// export ELSA_MYSQL_CONNECTION_STRING="Server=127.0.0.1;User ID=root;Password=$MYSQL_ROOT_PASSWORD;"
/// dotnet test --filter FullyQualifiedName~MySqlUniquenessMigrationTests
/// </code>
///
/// When the variable is missing they are skipped instead of reporting a green test that
/// never touched MySQL. Each test creates a throwaway database on that server and drops
/// it afterwards, so the connection string must grant database administration rights. No
/// duplicate rows are deleted by the migrations; the tests assert that.
/// </summary>
public class MySqlUniquenessMigrationTests
{
    private readonly ITestOutputHelper _output;

    public MySqlUniquenessMigrationTests(ITestOutputHelper output) => _output = output;

    private const string LabelBaselineMigration = "20250222190940_V3_4";
    private const string LabelUniquenessMigration = "20260913133332_PerTenantLabelUniqueness";
    private const string SecretBaselineMigration = "20260825225931_SecretTenancy";
    private const string SecretUniquenessMigration = "20260914120000_SecretDefaultTenantUniqueness";

    [MySqlFact]
    public async Task PerTenantLabelUniqueness_NoDuplicates_StampsNullsAndCreatesUniqueIndex()
    {
        var rootConnectionString = GetConnectionString();

        await WithDatabaseAsync(rootConnectionString, async databaseName =>
        {
            await using var context = CreateLabelsContext(rootConnectionString, databaseName);

            await MigrateToAsync(context, LabelBaselineMigration);
            await ExecuteAsync(context, """
                INSERT INTO `Labels` (`Id`, `Name`, `NormalizedName`, `TenantId`)
                VALUES
                    ('label-1', 'one', 'ONE', NULL),
                    ('label-2', 'two', 'TWO', NULL);
                """);

            await MigrateToAsync(context, LabelUniquenessMigration);

            // Nulls are stamped to the default tenant "" and the unique index exists.
            Assert.Equal(2, await ScalarOnDatabaseAsync(rootConnectionString, databaseName,
                "SELECT COUNT(*) FROM `Labels` WHERE `TenantId` = ''"));
            Assert.Equal(0, await ScalarOnDatabaseAsync(rootConnectionString, databaseName,
                "SELECT COUNT(*) FROM `Labels` WHERE `TenantId` IS NULL"));
            Assert.Equal(1, await MigrationAppliedCountAsync(rootConnectionString, databaseName, LabelUniquenessMigration));
            await AssertUniqueIndexAsync(rootConnectionString, databaseName, "Labels", "IX_Label_TenantId_NormalizedName");

            // The index is active for the default tenant, and per tenant:
            var duplicate = await Assert.ThrowsAsync<MySqlException>(() => ExecuteAsync(context, """
                INSERT INTO `Labels` (`Id`, `Name`, `NormalizedName`, `TenantId`)
                VALUES ('label-duplicate', 'duplicate', 'ONE', '');
                """));
            Assert.Equal(1062, duplicate.Number);

            await ExecuteAsync(context, """
                INSERT INTO `Labels` (`Id`, `Name`, `NormalizedName`, `TenantId`)
                VALUES ('label-other-tenant', 'other tenant', 'ONE', 'tenant-b');
                """);
        });
    }

    [MySqlFact]
    public async Task PerTenantLabelUniqueness_Duplicates_CreateIndexFailsWith1062AndKeepsRows()
    {
        var rootConnectionString = GetConnectionString();

        await WithDatabaseAsync(rootConnectionString, async databaseName =>
        {
            await using var context = CreateLabelsContext(rootConnectionString, databaseName);

            await MigrateToAsync(context, LabelBaselineMigration);
            await ExecuteAsync(context, """
                INSERT INTO `Labels` (`Id`, `Name`, `NormalizedName`, `TenantId`)
                VALUES
                    ('label-1', 'dup', 'DUPLICATE', NULL),
                    ('label-2', 'dup', 'DUPLICATE', NULL);
                """);

            var exception = await Assert.ThrowsAsync<MySqlException>(() => MigrateToAsync(context, LabelUniquenessMigration));
            _output.WriteLine($"MySQL error {exception.Number}: {exception.Message}");

            // MySQL's own message names the duplicate value and the index — the info the
            // removed preflight used to provide.
            Assert.Equal(1062, exception.Number);
            Assert.Contains("DUPLICATE", exception.Message, StringComparison.Ordinal);
            Assert.Contains("IX_Label_TenantId_NormalizedName", exception.Message, StringComparison.Ordinal);

            // Nothing is deleted and the migration did not apply.
            Assert.Equal(2, await ScalarOnDatabaseAsync(rootConnectionString, databaseName,
                "SELECT COUNT(*) FROM `Labels` WHERE `Id` IN ('label-1', 'label-2') AND `Name` = 'dup' AND `NormalizedName` = 'DUPLICATE'"));
            Assert.Equal(0, await MigrationAppliedCountAsync(rootConnectionString, databaseName, LabelUniquenessMigration));
            Assert.Equal(0, await IndexCountAsync(rootConnectionString, databaseName, "Labels", "IX_Label_TenantId_NormalizedName"));
        });
    }

    [MySqlFact]
    public async Task PerTenantLabelUniqueness_OverLengthNames_FailsBeforeAlter()
    {
        var rootConnectionString = GetConnectionString();

        await WithDatabaseAsync(rootConnectionString, async databaseName =>
        {
            await using var context = CreateLabelsContext(rootConnectionString, databaseName);

            await MigrateToAsync(context, LabelBaselineMigration);
            await ExecuteAsync(context, """
                INSERT INTO `Labels` (`Id`, `Name`, `NormalizedName`, `TenantId`)
                VALUES
                    ('label-name-over-length', REPEAT('n', 300), 'NORMALIZED', NULL),
                    ('label-normalized-over-length', 'NAME', REPEAT('N', 300), NULL);
                """);

            var exception = await Assert.ThrowsAsync<MySqlException>(() => MigrateToAsync(context, LabelUniquenessMigration));
            _output.WriteLine($"MySQL error {exception.Number}: {exception.Message}");

            Assert.Equal(1772, exception.Number);
            Assert.Contains("Over-length", exception.Message, StringComparison.Ordinal);
            Assert.Contains("label-name-over-length", exception.Message, StringComparison.Ordinal);
            Assert.Contains("label-normalized-over-length", exception.Message, StringComparison.Ordinal);
            Assert.Equal(0, await MigrationAppliedCountAsync(rootConnectionString, databaseName, LabelUniquenessMigration));
            Assert.Equal(0, await IndexCountAsync(rootConnectionString, databaseName, "Labels", "IX_Label_TenantId_NormalizedName"));
            Assert.Equal(2, await ScalarOnDatabaseAsync(rootConnectionString, databaseName,
                "SELECT COUNT(*) FROM `information_schema`.`columns` WHERE `table_schema` = DATABASE() AND `table_name` = 'Labels' AND `column_name` IN ('Name', 'NormalizedName') AND `data_type` = 'longtext'"));
        });
    }

    [MySqlFact]
    public async Task SecretDefaultTenantUniqueness_NoDuplicates_StampsNullsAndCreatesUniqueIndex()
    {
        var rootConnectionString = GetConnectionString();

        await WithDatabaseAsync(rootConnectionString, async databaseName =>
        {
            await using var context = CreateSecretsContext(rootConnectionString, databaseName);

            await MigrateToAsync(context, SecretBaselineMigration);
            await ExecuteAsync(context, """
                INSERT INTO `Secrets`
                    (`Id`, `Name`, `NormalizedName`, `DisplayName`, `TypeName`, `StoreName`, `Status`, `CreatedAt`, `Tags`, `Versions`, `TenantId`)
                VALUES
                    ('secret-1', 'one', 'ONE', 'one', 'type', 'store', 'Active', CURRENT_TIMESTAMP(6), '[]', '[]', NULL),
                    ('secret-2', 'two', 'TWO', 'two', 'type', 'store', 'Active', CURRENT_TIMESTAMP(6), '[]', '[]', NULL);
                """);

            await MigrateToAsync(context, SecretUniquenessMigration);

            Assert.Equal(2, await ScalarOnDatabaseAsync(rootConnectionString, databaseName,
                "SELECT COUNT(*) FROM `Secrets` WHERE `TenantId` = ''"));
            Assert.Equal(0, await ScalarOnDatabaseAsync(rootConnectionString, databaseName,
                "SELECT COUNT(*) FROM `Secrets` WHERE `TenantId` IS NULL"));
            Assert.Equal(1, await MigrationAppliedCountAsync(rootConnectionString, databaseName, SecretUniquenessMigration));
            await AssertUniqueIndexAsync(rootConnectionString, databaseName, "Secrets", "IX_Secret_TenantId_NormalizedName");

            var duplicate = await Assert.ThrowsAsync<MySqlException>(() => ExecuteAsync(context, """
                INSERT INTO `Secrets`
                    (`Id`, `Name`, `NormalizedName`, `DisplayName`, `TypeName`, `StoreName`, `Status`, `CreatedAt`, `Tags`, `Versions`, `TenantId`)
                VALUES ('secret-duplicate', 'duplicate', 'ONE', 'duplicate', 'type', 'store', 'Active', CURRENT_TIMESTAMP(6), '[]', '[]', '');
                """));
            Assert.Equal(1062, duplicate.Number);

            await ExecuteAsync(context, """
                INSERT INTO `Secrets`
                    (`Id`, `Name`, `NormalizedName`, `DisplayName`, `TypeName`, `StoreName`, `Status`, `CreatedAt`, `Tags`, `Versions`, `TenantId`)
                VALUES ('secret-other-tenant', 'other tenant', 'ONE', 'other tenant', 'type', 'store', 'Active', CURRENT_TIMESTAMP(6), '[]', '[]', 'tenant-b');
                """);
        });
    }

    [MySqlFact]
    public async Task SecretDefaultTenantUniqueness_Duplicates_CreateIndexFailsWith1062AndKeepsRows()
    {
        var rootConnectionString = GetConnectionString();

        await WithDatabaseAsync(rootConnectionString, async databaseName =>
        {
            await using var context = CreateSecretsContext(rootConnectionString, databaseName);

            await MigrateToAsync(context, SecretBaselineMigration);
            await ExecuteAsync(context, """
                INSERT INTO `Secrets`
                    (`Id`, `Name`, `NormalizedName`, `DisplayName`, `TypeName`, `StoreName`, `Status`, `CreatedAt`, `Tags`, `Versions`, `TenantId`)
                VALUES
                    ('secret-1', 'dup', 'DUPLICATE', 'dup', 'type', 'store', 'Active', CURRENT_TIMESTAMP(6), '[]', '[]', NULL),
                    ('secret-2', 'dup', 'DUPLICATE', 'dup', 'type', 'store', 'Active', CURRENT_TIMESTAMP(6), '[]', '[]', NULL);
                """);

            var exception = await Assert.ThrowsAsync<MySqlException>(() => MigrateToAsync(context, SecretUniquenessMigration));
            _output.WriteLine($"MySQL error {exception.Number}: {exception.Message}");

            Assert.Equal(1062, exception.Number);
            Assert.Contains("DUPLICATE", exception.Message, StringComparison.Ordinal);
            Assert.Contains("IX_Secret_TenantId_NormalizedName", exception.Message, StringComparison.Ordinal);

            Assert.Equal(2, await ScalarOnDatabaseAsync(rootConnectionString, databaseName,
                "SELECT COUNT(*) FROM `Secrets` WHERE `Id` IN ('secret-1', 'secret-2') AND `NormalizedName` = 'DUPLICATE'"));
            Assert.Equal(0, await MigrationAppliedCountAsync(rootConnectionString, databaseName, SecretUniquenessMigration));
            Assert.Equal(0, await IndexCountAsync(rootConnectionString, databaseName, "Secrets", "IX_Secret_TenantId_NormalizedName"));
        });
    }

    [MySqlFact]
    public async Task SecretDefaultTenantUniqueness_DuplicatesReconciled_RetrySucceeds()
    {
        var rootConnectionString = GetConnectionString();

        await WithDatabaseAsync(rootConnectionString, async databaseName =>
        {
            await using (var context = CreateSecretsContext(rootConnectionString, databaseName))
            {
                await MigrateToAsync(context, SecretBaselineMigration);
                await ExecuteAsync(context, """
                    INSERT INTO `Secrets`
                        (`Id`, `Name`, `NormalizedName`, `DisplayName`, `TypeName`, `StoreName`, `Status`, `CreatedAt`, `Tags`, `Versions`, `TenantId`)
                    VALUES
                        ('secret-1', 'dup', 'DUPLICATE', 'dup', 'type', 'store', 'Active', CURRENT_TIMESTAMP(6), '[]', '[]', NULL),
                        ('secret-2', 'dup', 'DUPLICATE', 'dup', 'type', 'store', 'Active', CURRENT_TIMESTAMP(6), '[]', '[]', NULL);
                    """);

                var exception = await Assert.ThrowsAsync<MySqlException>(() => MigrateToAsync(context, SecretUniquenessMigration));
                _output.WriteLine($"MySQL error {exception.Number}: {exception.Message}");
                Assert.Equal(1062, exception.Number);
            }

            // MySQL commits the index drop before CREATE UNIQUE INDEX fails, so the
            // index is already gone while EF leaves the migration unapplied.
            Assert.Equal(0, await MigrationAppliedCountAsync(rootConnectionString, databaseName, SecretUniquenessMigration));
            Assert.Equal(0, await IndexCountAsync(rootConnectionString, databaseName, "Secrets", "IX_Secret_TenantId_NormalizedName"));

            await using (var repairContext = CreateSecretsContext(rootConnectionString, databaseName))
            {
                await ExecuteAsync(repairContext, """
                    UPDATE `Secrets`
                    SET `NormalizedName` = 'DUPLICATE-2'
                    WHERE `Id` = 'secret-2';
                    """);
            }

            await using (var retryContext = CreateSecretsContext(rootConnectionString, databaseName))
            {
                await MigrateToAsync(retryContext, SecretUniquenessMigration);
            }

            Assert.Equal(1, await MigrationAppliedCountAsync(rootConnectionString, databaseName, SecretUniquenessMigration));
            await AssertUniqueIndexAsync(rootConnectionString, databaseName, "Secrets", "IX_Secret_TenantId_NormalizedName");
            Assert.Equal(2, await ScalarOnDatabaseAsync(rootConnectionString, databaseName,
                "SELECT COUNT(*) FROM `Secrets` WHERE `Id` IN ('secret-1', 'secret-2') AND `TenantId` = ''"));
        });
    }

    /// <summary>
    /// A least-privilege migration user (DDL/DML only, no CREATE ROUTINE / ALTER ROUTINE)
    /// must be able to run the clean path — #9114 was partly about not adding routine
    /// privileges for a patch upgrade.
    /// </summary>
    [MySqlFact]
    public async Task UniquenessMigrations_DdlDmlOnlyMigrationUser_MigratesCleanDatabase()
    {
        var rootConnectionString = GetConnectionString();

        await WithDatabaseAsync(rootConnectionString, async databaseName =>
        {
            // MySQL caps user names at 32 characters, so the GUID is shortened.
            var userName = $"elsa_9114_{Guid.NewGuid():N}"[..24];
            var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var databaseBuilder = new MySqlConnectionStringBuilder(rootConnectionString) { Database = databaseName };
            var restrictedConnectionString = new MySqlConnectionStringBuilder(databaseBuilder.ConnectionString)
            {
                UserID = userName,
                Password = password
            }.ConnectionString;

            await using var adminConnection = new MySqlConnection(rootConnectionString);
            await adminConnection.OpenAsync();

            // MySQL 8 removed GRANT ... IDENTIFIED BY, so the login and its grants are separate.
            await ExecuteOnAsync(adminConnection, $"CREATE USER '{userName}'@'%' IDENTIFIED BY '{password}';");

            try
            {
                await ExecuteOnAsync(adminConnection, $"GRANT CREATE, ALTER, DROP, INDEX, REFERENCES, SELECT, INSERT, UPDATE, DELETE ON `{databaseName}`.* TO '{userName}'@'%';");

                // DDL/DML only: no routine privileges at all, so a regression back to a stored
                // procedure (or SIGNAL through PREPARE, the #9114 failure) cannot pass silently.
                var grants = string.Join(Environment.NewLine,
                    await ReadStringsOnAsync(adminConnection, $"SHOW GRANTS FOR '{userName}'@'%'"));
                Assert.DoesNotContain("ROUTINE", grants, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("CREATE", grants, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("ALTER", grants, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("SELECT", grants, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("INSERT", grants, StringComparison.OrdinalIgnoreCase);

                await using var labelsContext = CreateLabelsContext(restrictedConnectionString, databaseName);
                await MigrateToAsync(labelsContext, LabelUniquenessMigration);
                await using var secretsContext = CreateSecretsContext(restrictedConnectionString, databaseName);
                await MigrateToAsync(secretsContext, SecretUniquenessMigration);

                Assert.Equal(1, await MigrationAppliedCountAsync(rootConnectionString, databaseName, LabelUniquenessMigration));
                Assert.Equal(1, await MigrationAppliedCountAsync(rootConnectionString, databaseName, SecretUniquenessMigration));
                await AssertUniqueIndexAsync(rootConnectionString, databaseName, "Labels", "IX_Label_TenantId_NormalizedName");
                await AssertUniqueIndexAsync(rootConnectionString, databaseName, "Secrets", "IX_Secret_TenantId_NormalizedName");
            }
            finally
            {
                await ExecuteOnAsync(adminConnection, $"DROP USER IF EXISTS '{userName}'@'%';");
            }
        });
    }

    private static string GetConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable("ELSA_MYSQL_CONNECTION_STRING");
        return !string.IsNullOrWhiteSpace(connectionString)
            ? connectionString
            : throw new InvalidOperationException(
                $"Set ELSA_MYSQL_CONNECTION_STRING to run {nameof(MySqlUniquenessMigrationTests)} against a real MySQL server.");
    }

    private static async Task WithDatabaseAsync(string rootConnectionString, Func<string, Task> action)
    {
        // MySQL lowercases identifiers; use a short ASCII name.
        var databaseName = $"elsa_9114_{Guid.NewGuid():N}";
        await using var adminConnection = new MySqlConnection(rootConnectionString);
        await adminConnection.OpenAsync();
        await ExecuteOnAsync(adminConnection, $"CREATE DATABASE `{databaseName}`;");

        try
        {
            await action(databaseName);
        }
        finally
        {
            await ExecuteOnAsync(adminConnection, $"DROP DATABASE IF EXISTS `{databaseName}`;");
        }
    }

    private static LabelsElsaDbContext CreateLabelsContext(string connectionString, string databaseName)
    {
        var databaseConnectionString = new MySqlConnectionStringBuilder(connectionString) { Database = databaseName }.ConnectionString;
        var optionsBuilder = new DbContextOptionsBuilder<LabelsElsaDbContext>();
        optionsBuilder.UseElsaMySql(
            typeof(Elsa.Persistence.EFCore.MySql.Migrations.Labels.PerTenantLabelUniqueness).Assembly,
            databaseConnectionString);
        return new(optionsBuilder.Options, new ServiceCollection().BuildServiceProvider());
    }

    private static SecretsElsaDbContext CreateSecretsContext(string connectionString, string databaseName)
    {
        var databaseConnectionString = new MySqlConnectionStringBuilder(connectionString) { Database = databaseName }.ConnectionString;
        var optionsBuilder = new DbContextOptionsBuilder<SecretsElsaDbContext>();
        optionsBuilder.UseElsaMySql(
            typeof(Elsa.Secrets.Persistence.EFCore.MySql.Migrations.Secrets.SecretDefaultTenantUniqueness).Assembly,
            databaseConnectionString);
        return new(optionsBuilder.Options, new ServiceCollection().BuildServiceProvider());
    }

    private static Task MigrateToAsync(DbContext context, string migrationId) =>
        context.GetService<IMigrator>().MigrateAsync(migrationId);

    private static async Task ExecuteAsync(DbContext context, string sql)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
            await connection.OpenAsync();

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }

    private static async Task ExecuteOnAsync(MySqlConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<List<string>> ReadStringsOnAsync(MySqlConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var results = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            results.Add(reader.GetString(0));
        return results;
    }

    private static async Task<int> ScalarOnDatabaseAsync(string rootConnectionString, string databaseName, string sql)
    {
        var builder = new MySqlConnectionStringBuilder(rootConnectionString) { Database = databaseName };
        await using var connection = new MySqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static async Task<int> MigrationAppliedCountAsync(string rootConnectionString, string databaseName, string migrationId)
    {
        var builder = new MySqlConnectionStringBuilder(rootConnectionString) { Database = databaseName };
        await using var connection = new MySqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM `__EFMigrationsHistory` WHERE `MigrationId` = @migrationId";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@migrationId";
        parameter.Value = migrationId;
        command.Parameters.Add(parameter);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static Task<int> IndexCountAsync(string rootConnectionString, string databaseName, string table, string indexName) =>
        ScalarOnDatabaseAsync(rootConnectionString, databaseName, $"""
            SELECT COUNT(*) FROM `information_schema`.`statistics`
            WHERE `table_schema` = DATABASE()
              AND `table_name` = '{table}'
              AND `index_name` = '{indexName}'
            """);

    private static async Task AssertUniqueIndexAsync(string rootConnectionString, string databaseName, string table, string indexName)
    {
        var coveredColumns = await ScalarOnDatabaseAsync(rootConnectionString, databaseName, $"""
            SELECT COUNT(*) FROM `information_schema`.`statistics`
            WHERE `table_schema` = DATABASE()
              AND `table_name` = '{table}'
              AND `index_name` = '{indexName}'
              AND `column_name` IN ('TenantId', 'NormalizedName')
            """);
        var nonUniqueColumns = await ScalarOnDatabaseAsync(rootConnectionString, databaseName, $"""
            SELECT COUNT(*) FROM `information_schema`.`statistics`
            WHERE `table_schema` = DATABASE()
              AND `table_name` = '{table}'
              AND `index_name` = '{indexName}'
              AND `column_name` IN ('TenantId', 'NormalizedName')
              AND `non_unique` = 1
            """);

        Assert.Equal(2, coveredColumns);
        Assert.Equal(0, nonUniqueColumns);
    }
}

/// <summary>
/// Skips the MySQL integration tests when <c>ELSA_MYSQL_CONNECTION_STRING</c> is absent.
/// xUnit 2.x has no dynamic skip at execution time, but a <see cref="FactAttribute"/>
/// subclass can set <see cref="FactAttribute.Skip"/> during discovery. That keeps the
/// default CI run green without pretending the tests ran against MySQL.
/// </summary>
internal sealed class MySqlFactAttribute : FactAttribute
{
    public MySqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ELSA_MYSQL_CONNECTION_STRING")))
        {
            Skip = $"Set ELSA_MYSQL_CONNECTION_STRING to run {nameof(MySqlUniquenessMigrationTests)} against a real MySQL server.";
        }
    }
}
