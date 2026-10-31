using System.Security.Cryptography;
using System.Reflection;
using System.Text.Json;
using Elsa.Secrets.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: ExtensionsRunner <migrate|inspect> <database-path>");
    return 64;
}

var action = args[0];
var databasePath = Path.GetFullPath(args[1]);
var connectionString = $"Data Source={databasePath}";
var optionsBuilder = new DbContextOptionsBuilder<SecretsDbContext>()
    // Mirrors the standard Extensions graph feature configuration; see the evidence in README.md.
    .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
optionsBuilder.UseElsaSqlite(Assembly.Load("Elsa.Secrets.Persistence.EFCore.Sqlite"), connectionString);
var options = optionsBuilder.Options;

using var serviceProvider = new ServiceCollection().BuildServiceProvider();
using var context = new SecretsDbContext(options, serviceProvider);

if (action == "migrate")
{
    await context.Database.MigrateAsync();
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        phase = "extensions-3.8.1",
        result = "migrated",
        appliedMigrations = await context.Database.GetAppliedMigrationsAsync()
    }));
    return 0;
}

if (action == "inspect")
{
    var rows = await context.Secrets.AsNoTracking()
        .OrderBy(secret => secret.Version)
        .Select(secret => new
        {
            secret.SecretId,
            secret.Version,
            secret.IsLatest,
            secret.Status,
            secret.TenantId,
            secret.Owner,
            secret.ExpiresIn,
            secret.ExpiresAt,
            secret.EncryptedValue
        })
        .ToListAsync();
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        phase = "extensions-3.8.1",
        result = "readable",
        rowCount = rows.Count,
        versions = rows.Select(row => row.Version),
        secretIds = rows.Select(row => row.SecretId).Distinct(),
        latestFlags = rows.Select(row => row.IsLatest),
        encryptedValueSha256 = rows.Select(row => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(row.EncryptedValue))))
    }));
    return 0;
}

Console.Error.WriteLine($"Unknown action: {action}");
return 64;
