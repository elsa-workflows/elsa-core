using System.Reflection;
using System.Text.Json;
using Elsa.Secrets.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

if (args.Length != 2 || args[0] != "upgrade")
{
    Console.Error.WriteLine("Usage: CoreRunner upgrade <database-path>");
    return 64;
}

var databasePath = Path.GetFullPath(args[1]);
var connectionString = $"Data Source={databasePath}";
var optionsBuilder = new DbContextOptionsBuilder<SecretsElsaDbContext>()
    .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
optionsBuilder.UseElsaSqlite(Assembly.Load("Elsa.Secrets.Persistence.EFCore.Sqlite"), connectionString);
var options = optionsBuilder.Options;

using var serviceProvider = new ServiceCollection().BuildServiceProvider();
using var context = new SecretsElsaDbContext(options, serviceProvider);
var appliedBeforeUpgrade = await context.Database.GetAppliedMigrationsAsync();
var pendingBeforeUpgrade = await context.Database.GetPendingMigrationsAsync();

try
{
    await context.Database.MigrateAsync();
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        phase = "core-3.8.4",
        result = "migrated",
        appliedMigrationsBeforeUpgrade = appliedBeforeUpgrade,
        pendingMigrationsBeforeUpgrade = pendingBeforeUpgrade,
        appliedMigrations = await context.Database.GetAppliedMigrationsAsync()
    }));
}
catch (Exception exception)
{
    // Migration failure is a valid characterization result. The driver reopens
    // the database with the old package graph and checks the seeded rows.
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        phase = "core-3.8.4",
        result = "failed",
        appliedMigrationsBeforeUpgrade = appliedBeforeUpgrade,
        pendingMigrationsBeforeUpgrade = pendingBeforeUpgrade,
        exceptionType = exception.GetType().FullName,
        exceptionMessage = exception.Message
    }));
}

return 0;
