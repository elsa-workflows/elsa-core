using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Secrets.Management;
using Elsa.Secrets;
using Elsa.Secrets.Persistence.EFCore;
using Elsa.Secrets.Persistence.EFCore.Sqlite;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;
using Secret = Elsa.Secrets.Management.Secret;

const string applicationName = "Elsa-Secrets-Bridge-Contract-Synthetic";
const string purpose = "Elsa.Secrets.Encryption";

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: ExtensionsCryptoRunner seed|verify <database-path> <key-ring-dir> [wrong-key-ring-dir] [ciphertext-path]");
    return 64;
}

var databasePath = Path.GetFullPath(args[1]);
Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

if (args[0] == "seed")
{
    if (args.Length != 5)
    {
        Console.Error.WriteLine("Usage: ExtensionsCryptoRunner seed <database-path> <key-ring-dir> <wrong-key-ring-dir> <ciphertext-path>");
        return 64;
    }

    if (File.Exists(databasePath))
        File.Delete(databasePath);

    var normalMigrateDiagnostic = await ProbeStandardMigrationAsync(databasePath + ".standard-migrate-probe");
    var plainRows = CreateRows();
    using var oldServices = CreateDataProtectionServices(args[2], applicationName);
    using var wrongServices = CreateDataProtectionServices(args[3], applicationName);
    var encryptor = new DataProtectionEncryptor(oldServices.GetRequiredService<IDataProtectionProvider>());
    foreach (var row in plainRows)
        row.EncryptedValue = await encryptor.EncryptAsync(row.Plaintext);
    await File.WriteAllTextAsync(args[4], JsonSerializer.Serialize(plainRows.Take(2).Select(row => row.EncryptedValue)));

    _ = wrongServices.GetRequiredService<IDataProtectionProvider>()
        .CreateProtector(purpose)
        .Protect("Synthetic wrong-key-ring seed");

    var migrationSqlSha256 = string.Empty;
    await using (var services = CreateDatabaseServices(databasePath))
    await using (var scope = services.CreateAsyncScope())
    {
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SecretsDbContext>>();
        await using var dbContext = await factory.CreateDbContextAsync();
        var migrationSql = dbContext.GetService<IMigrator>().GenerateScript();
        migrationSqlSha256 = Hash(migrationSql);
        await dbContext.Database.ExecuteSqlRawAsync(migrationSql);
        var migrations = (await dbContext.Database.GetAppliedMigrationsAsync()).ToArray();
        if (!migrations.SequenceEqual(["20240915164114_V3_3"], StringComparer.Ordinal))
            throw new InvalidOperationException("The old runner did not apply the pinned V3_3 migration.");

        await dbContext.Secrets.AddRangeAsync(plainRows.Select(row => row.Secret));
        await dbContext.SaveChangesAsync();

        var storedRows = await dbContext.Secrets.AsNoTracking().ToListAsync();
        if (storedRows.Count != plainRows.Length || storedRows.Any(row => string.IsNullOrWhiteSpace(row.EncryptedValue)))
            throw new InvalidOperationException("The old runner did not persist every synthetic encrypted row.");
    }

    Console.WriteLine(JsonSerializer.Serialize(new
    {
        phase = "extensions-3.8.1",
        result = "seeded",
        schemaMaterialization = "generated-sql-from-pinned-migrator",
        migrationSqlSha256,
        normalMigrateDiagnostic,
        migrationIds = new[] { "20240915164114_V3_3" },
        syntheticRows = plainRows.Length,
        syntheticVersions = 2,
        plaintextSha256 = plainRows.Take(2).Select(row => Hash(row.Plaintext)).ToArray(),
        sourceRowIds = plainRows.Select(row => row.Id).ToArray(),
        sourceAggregateIds = plainRows.Select(row => row.SecretId).ToArray(),
        dataProtectionPurpose = purpose,
        applicationName
    }));
    return 0;
}

if (args[0] == "verify")
{
    if (args.Length != 3)
    {
        Console.Error.WriteLine("Usage: ExtensionsCryptoRunner verify <database-path> <key-ring-dir>");
        return 64;
    }

    using var services = CreateDatabaseServices(databasePath, readOnly: true);
    using var oldServices = CreateDataProtectionServices(args[2], applicationName);
    var decryptor = oldServices.GetRequiredService<IDataProtectionProvider>().CreateProtector(purpose);
    await using var scope = services.CreateAsyncScope();
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SecretsDbContext>>();
    await using var dbContext = await factory.CreateDbContextAsync();
    var migrations = (await dbContext.Database.GetAppliedMigrationsAsync()).ToArray();
    var rows = await dbContext.Secrets.AsNoTracking().OrderBy(row => row.SecretId).ThenBy(row => row.Version).ToListAsync();
    var hashes = rows.Select(row => Hash(decryptor.Unprotect(row.EncryptedValue))).ToArray();
    var expectedHashes = CreateRows().Select(row => Hash(row.Plaintext)).Order(StringComparer.Ordinal).ToArray();

    var readable = migrations.SequenceEqual(["20240915164114_V3_3"], StringComparer.Ordinal)
        && rows.Count == CreateRows().Length
        && hashes.Order(StringComparer.Ordinal).SequenceEqual(expectedHashes, StringComparer.Ordinal);
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        phase = "extensions-3.8.1",
        result = readable ? "reopened-and-verified" : "verification-failed",
        migrationIds = migrations,
        syntheticRows = rows.Count,
        plaintextSha256 = hashes.Order(StringComparer.Ordinal).ToArray(),
        sourceUnchangedReadable = readable
    }));
    return readable ? 0 : 1;
}

Console.Error.WriteLine($"Unknown phase '{args[0]}'.");
return 64;

static ServiceProvider CreateDatabaseServices(string databasePath, bool readOnly = false)
{
    var connectionStringBuilder = new SqliteConnectionStringBuilder
    {
        DataSource = databasePath,
        Mode = readOnly ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWriteCreate,
        Pooling = false
    };
    var services = new ServiceCollection();
    services.AddSqliteEntityModelCreatingHandlers();
    services.AddDbContextFactory<SecretsDbContext>(builder =>
        builder.UseElsaSqlite(typeof(SqliteSecretsDbContextFactory).Assembly, connectionStringBuilder.ToString()));
    return services.BuildServiceProvider();
}

static async Task<string> ProbeStandardMigrationAsync(string databasePath)
{
    DeleteSqliteFiles(databasePath);
    try
    {
        using var services = CreateDatabaseServices(databasePath);
        await using var scope = services.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SecretsDbContext>>();
        await using var dbContext = await factory.CreateDbContextAsync();
        await dbContext.Database.MigrateAsync();
        return "migrated";
    }
    catch (InvalidOperationException error) when (error.Message.Contains("PendingModelChangesWarning", StringComparison.Ordinal))
    {
        return "blocked-by-pending-model-changes-warning";
    }
    finally
    {
        DeleteSqliteFiles(databasePath);
    }
}

static void DeleteSqliteFiles(string path)
{
    foreach (var suffix in new[] { "", "-wal", "-shm" })
    {
        var candidate = path + suffix;
        if (File.Exists(candidate))
            File.Delete(candidate);
    }
}

static ServiceProvider CreateDataProtectionServices(string keyRingDirectory, string appName)
{
    Directory.CreateDirectory(keyRingDirectory);
    var services = new ServiceCollection();
    services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keyRingDirectory))
        .SetApplicationName(appName);
    return services.BuildServiceProvider();
}

static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

static FixtureRow[] CreateRows()
{
    var utc = TimeSpan.Zero;
    var created = new DateTimeOffset(2026, 9, 1, 12, 0, 0, utc);
    var updated = created.AddHours(1);
    return
    [
        new("legacy-row-default-v1", "legacy-aggregate-default", "shared:service", null, "owner-default", 1, false, SecretStatus.Retired, "Synthetic default tenant secret version one", created, updated, "00:15:00", created.AddDays(30), created.AddMinutes(5)),
        new("legacy-row-default-v2", "legacy-aggregate-default", "shared:service", string.Empty, "owner-default", 2, true, SecretStatus.Active, "Synthetic default tenant secret version two", created.AddDays(1), updated.AddDays(1), "01:00:00", created.AddDays(60), created.AddDays(1).AddMinutes(5)),
        new("legacy-row-tenant-a-v1", "legacy-aggregate-tenant-a", "shared:service", "tenant-a", "owner-a", 1, true, SecretStatus.Expired, "Synthetic tenant A secret value", created, updated, "00:30:00", created.AddMinutes(30), created.AddMinutes(10)),
        new("legacy-row-tenant-b-v1", "legacy-aggregate-tenant-b", "shared:service", "tenant-b", "owner-b", 1, true, SecretStatus.Revoked, "Synthetic tenant B secret value", created, updated, "00:45:00", created.AddMinutes(45), created.AddMinutes(15)),
        new("legacy-row-tenant-b-exclusive-v1", "legacy-aggregate-tenant-b-exclusive", "tenant-b:exclusive", "tenant-b", "owner-b", 1, true, SecretStatus.Active, "Synthetic tenant B exclusive value", created, updated, "00:45:00", created.AddMinutes(45), created.AddMinutes(15))
    ];
}

sealed class FixtureRow(
    string id,
    string secretId,
    string name,
    string? tenantId,
    string owner,
    int version,
    bool isLatest,
    SecretStatus status,
    string plaintext,
    DateTimeOffset createdAt,
    DateTimeOffset updatedAt,
    string expiresIn,
    DateTimeOffset expiresAt,
    DateTimeOffset lastAccessedAt)
{
    public string Id { get; } = id;
    public string SecretId { get; } = secretId;
    public string Plaintext { get; } = plaintext;
    public string EncryptedValue { get => Secret.EncryptedValue; set => Secret.EncryptedValue = value; }
    public Secret Secret { get; } = new()
    {
        Id = id,
        SecretId = secretId,
        Name = name,
        Scope = "credential",
        EncryptedValue = "pending-encryption",
        Description = $"Synthetic {secretId} version {version}",
        Version = version,
        IsLatest = isLatest,
        Status = status,
        ExpiresIn = TimeSpan.Parse(expiresIn),
        ExpiresAt = expiresAt,
        LastAccessedAt = lastAccessedAt,
        TenantId = tenantId,
        CreatedAt = createdAt,
        UpdatedAt = updatedAt,
        Owner = owner
    };
}
