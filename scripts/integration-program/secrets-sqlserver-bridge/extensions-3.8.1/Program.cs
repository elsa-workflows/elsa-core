using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Secrets;
using Elsa.Secrets.Management;
using Elsa.Secrets.Persistence.EFCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Secret = Elsa.Secrets.Management.Secret;

const string purpose = "Elsa.Secrets.Encryption";
const string applicationName = "Elsa-Secrets-Bridge-Contract-Synthetic";
string[] expectedPlaintext =
[
    "Synthetic default tenant secret version one",
    "Synthetic default tenant secret version two",
    "Synthetic tenant A secret value",
    "Synthetic tenant B secret value",
    "Synthetic tenant B exclusive value"
];

if (args.Length < 2 || args[0] is not ("seed" or "inspect" or "snapshot"))
{
    Console.Error.WriteLine("Usage: ExtensionsRunner <seed|inspect> <connection-string> [key-ring-dir]");
    return 64;
}

var options = new DbContextOptionsBuilder<SecretsDbContext>()
    .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
options.UseElsaSqlServer(Assembly.Load("Elsa.Secrets.Persistence.EFCore.SqlServer"), args[1]);
using var provider = new ServiceCollection().BuildServiceProvider();
using var context = new SecretsDbContext(options.Options, provider);

if (args[0] == "seed")
{
    if (args.Length != 3)
        return 64;

    var keyRingPath = Path.GetFullPath(args[2]);
    Directory.CreateDirectory(keyRingPath);
    var protector = DataProtectionProvider.Create(new DirectoryInfo(keyRingPath), builder => builder.SetApplicationName(applicationName))
        .CreateProtector(purpose);
    await context.Database.MigrateAsync();
    var rows = CreateRows(protector, expectedPlaintext);
    await context.Secrets.AddRangeAsync(rows);
    await context.SaveChangesAsync();
    var storedRows = await context.Secrets.AsNoTracking().OrderBy(row => row.Id).ToArrayAsync();
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        phase = "extensions-3.8.1-sqlserver",
        result = "seeded",
        appliedMigrations = await context.Database.GetAppliedMigrationsAsync(),
        sourceRows = storedRows.Length,
        sourceAggregateIds = storedRows.Select(row => row.SecretId).Distinct().Order(StringComparer.Ordinal),
        plaintextSha256 = expectedPlaintext.Select(Hash).Order(StringComparer.Ordinal),
        sourceRowHashSha256 = HashRows(storedRows),
        ciphertextSha256 = storedRows.Select(row => Hash(row.EncryptedValue)).Order(StringComparer.Ordinal),
        expiresInMaximumTicks = storedRows.Where(row => row.ExpiresIn.HasValue).Max(row => row.ExpiresIn!.Value.Ticks),
        expiresInAllRepresentableBySqlTime = storedRows.All(row => row.ExpiresIn is null || row.ExpiresIn.Value >= TimeSpan.Zero && row.ExpiresIn.Value < TimeSpan.FromDays(1)),
        plaintextPrinted = false,
        keysPrinted = false,
        ciphertextPrinted = false
    }));
    return 0;
}

var actualMigrations = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
var source = await context.Secrets.AsNoTracking().OrderBy(row => row.SecretId).ThenBy(row => row.Version).ToArrayAsync();
if (args[0] == "snapshot")
{
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        phase = "extensions-3.8.1-sqlserver",
        result = "snapshotted",
        appliedMigrations = actualMigrations,
        sourceRows = source.Length,
        sourceRowHashSha256 = HashRows(source),
        ciphertextSha256 = source.Select(row => Hash(row.EncryptedValue)).Order(StringComparer.Ordinal)
    }));
    return 0;
}

if (args.Length != 3)
    return 64;

var readProtector = DataProtectionProvider.Create(new DirectoryInfo(Path.GetFullPath(args[2])), builder => builder.SetApplicationName(applicationName))
    .CreateProtector(purpose);
var recoveredHashes = source.Select(row => Hash(readProtector.Unprotect(row.EncryptedValue))).Order(StringComparer.Ordinal).ToArray();
var expectedHashes = expectedPlaintext.Select(Hash).Order(StringComparer.Ordinal).ToArray();
var readable = actualMigrations.SequenceEqual(["20241011092820_V3_3"], StringComparer.Ordinal)
    && source.Length == expectedPlaintext.Length
    && source.All(row => row.ExpiresIn is null || row.ExpiresIn.Value >= TimeSpan.Zero && row.ExpiresIn.Value < TimeSpan.FromDays(1))
    && recoveredHashes.SequenceEqual(expectedHashes, StringComparer.Ordinal);
Console.WriteLine(JsonSerializer.Serialize(new
{
    phase = "extensions-3.8.1-sqlserver",
    result = readable ? "readable" : "verification-failed",
    appliedMigrations = actualMigrations,
    sourceRows = source.Length,
    sourceRowIdsSha256 = source.Select(row => Hash(row.Id)).Order(StringComparer.Ordinal),
    sourceRowHashSha256 = HashRows(source),
    ciphertextSha256 = source.Select(row => Hash(row.EncryptedValue)).Order(StringComparer.Ordinal),
    plaintextSha256 = recoveredHashes,
    expiresInMaximumTicks = source.Where(row => row.ExpiresIn.HasValue).Max(row => row.ExpiresIn!.Value.Ticks),
    expiresInAllRepresentableBySqlTime = source.All(row => row.ExpiresIn is null || row.ExpiresIn.Value >= TimeSpan.Zero && row.ExpiresIn.Value < TimeSpan.FromDays(1)),
    sourceUnchangedReadable = readable,
    plaintextPrinted = false,
    keysPrinted = false,
    ciphertextPrinted = false
}));
return readable ? 0 : 1;

static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

static string HashRows(IEnumerable<Secret> rows)
{
    var normalized = rows.OrderBy(row => row.Id, StringComparer.Ordinal).Select(row => new
    {
        row.Id,
        row.SecretId,
        row.Name,
        row.Scope,
        encryptedValueSha256 = Hash(row.EncryptedValue),
        row.Description,
        row.Version,
        row.IsLatest,
        row.Status,
        expiresIn = row.ExpiresIn?.ToString("c", System.Globalization.CultureInfo.InvariantCulture),
        expiresAt = row.ExpiresAt?.ToUniversalTime().ToString("O", System.Globalization.CultureInfo.InvariantCulture),
        lastAccessedAt = row.LastAccessedAt?.ToUniversalTime().ToString("O", System.Globalization.CultureInfo.InvariantCulture),
        row.TenantId,
        createdAt = row.CreatedAt.ToUniversalTime().ToString("O", System.Globalization.CultureInfo.InvariantCulture),
        updatedAt = row.UpdatedAt.ToUniversalTime().ToString("O", System.Globalization.CultureInfo.InvariantCulture),
        row.Owner
    });
    return Hash(JsonSerializer.Serialize(normalized));
}

static Secret[] CreateRows(IDataProtector protector, IReadOnlyList<string> expectedPlaintext)
{
    var created = new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);
    var rows = new (string Id, string SecretId, string Name, string? TenantId, string Owner, int Version, bool Latest, int Status, string Plaintext, TimeSpan? ExpiresIn, DateTimeOffset? ExpiresAt, DateTimeOffset? LastAccessedAt)[]
    {
        ("legacy-row-default-v1", "legacy-aggregate-default", "shared:service", null, "owner-default", 1, false, 1, expectedPlaintext[0], TimeSpan.FromMinutes(15), created.AddDays(30), created.AddMinutes(5)),
        ("legacy-row-default-v2", "legacy-aggregate-default", "shared:service", null, "owner-default", 2, true, 0, expectedPlaintext[1], TimeSpan.FromTicks(TimeSpan.TicksPerDay - 1), created.AddDays(60), created.AddDays(1).AddMinutes(5)),
        ("legacy-row-tenant-a-v1", "legacy-aggregate-tenant-a", "shared:service", "tenant-a", "owner-a", 1, true, 2, expectedPlaintext[2], TimeSpan.FromMinutes(30), created.AddMinutes(30), created.AddMinutes(10)),
        ("legacy-row-tenant-b-v1", "legacy-aggregate-tenant-b", "shared:service", "tenant-b", "owner-b", 1, true, 3, expectedPlaintext[3], TimeSpan.FromHours(23), created.AddMinutes(45), created.AddMinutes(15)),
        ("legacy-row-tenant-b-exclusive-v1", "legacy-aggregate-tenant-b-exclusive", "tenant-b:exclusive", "tenant-b", "owner-b", 1, true, 0, expectedPlaintext[4], null, null, null)
    };

    return rows.Select(row => new Secret
    {
        Id = row.Id,
        SecretId = row.SecretId,
        Name = row.Name,
        Scope = "credential",
        EncryptedValue = protector.Protect(row.Plaintext),
        Description = $"Synthetic {row.SecretId} version {row.Version}",
        Version = row.Version,
        IsLatest = row.Latest,
        Status = (SecretStatus)row.Status,
        ExpiresIn = row.ExpiresIn,
        ExpiresAt = row.ExpiresAt,
        LastAccessedAt = row.LastAccessedAt,
        TenantId = row.TenantId,
        CreatedAt = created.AddDays(row.Version == 2 ? 1 : 0),
        UpdatedAt = created.AddHours(row.Version == 2 ? 25 : 1),
        Owner = row.Owner
    }).ToArray();
}
