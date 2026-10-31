using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Secrets.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

if (args.Length != 2 || args[0] is not ("migrate" or "seed" or "snapshot" or "inspect"))
{
    Console.Error.WriteLine("Usage: ExtensionsRunner <migrate|seed|snapshot|inspect> <connection-string>");
    return 64;
}

var optionsBuilder = new DbContextOptionsBuilder<SecretsDbContext>()
    // Mirrors the released Extensions graph feature configuration.
    .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
optionsBuilder.UseElsaSqlServer(Assembly.Load("Elsa.Secrets.Persistence.EFCore.SqlServer"), args[1]);

using var serviceProvider = new ServiceCollection().BuildServiceProvider();
using var context = new SecretsDbContext(optionsBuilder.Options, serviceProvider);

if (args[0] == "migrate")
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

if (args[0] == "seed")
{
    var count = await SqlServerSnapshot.SeedAsync(args[1]);
    Console.WriteLine(JsonSerializer.Serialize(new { phase = "extensions-3.8.1", result = "seeded", rowCount = count }));
    return 0;
}

if (args[0] == "snapshot")
{
    Console.WriteLine(JsonSerializer.Serialize(await SqlServerSnapshot.CaptureAsync(args[1])));
    return 0;
}

var rows = await context.Secrets.AsNoTracking()
    .OrderBy(secret => secret.Version)
    .Select(secret => new
    {
        secret.Id,
        secret.SecretId,
        secret.Name,
        secret.Scope,
        secret.Description,
        secret.Version,
        secret.IsLatest,
        secret.Status,
        secret.TenantId,
        secret.Owner,
        secret.ExpiresIn,
        secret.ExpiresAt,
        secret.LastAccessedAt,
        secret.CreatedAt,
        secret.UpdatedAt,
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
    encryptedValueSha256 = rows.Select(row => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(row.EncryptedValue))))
}));
return 0;
