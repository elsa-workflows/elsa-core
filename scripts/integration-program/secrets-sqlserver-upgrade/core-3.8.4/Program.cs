using System.Reflection;
using System.Text.Json;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Secrets.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.SqlClient;

if (args.Length != 2 || args[0] != "upgrade")
{
    Console.Error.WriteLine("Usage: CoreRunner upgrade <connection-string>");
    return 64;
}

var optionsBuilder = new DbContextOptionsBuilder<SecretsElsaDbContext>()
    // Mirrors the Core 3.8.4 persistence shell feature configuration.
    .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
optionsBuilder.UseElsaSqlServer(Assembly.Load("Elsa.Secrets.Persistence.EFCore.SqlServer"), args[1]);

using var serviceProvider = new ServiceCollection().BuildServiceProvider();
using var context = new SecretsElsaDbContext(optionsBuilder.Options, serviceProvider);
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
catch (SqlException exception)
{
    // A failed migration is characterized by reopening the same database with
    // the released package and comparing its schema, history, and seeded rows.
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        phase = "core-3.8.4",
        result = "failed",
        appliedMigrationsBeforeUpgrade = appliedBeforeUpgrade,
        pendingMigrationsBeforeUpgrade = pendingBeforeUpgrade,
        exceptionType = exception.GetType().FullName,
        exceptionMessage = exception.Message,
        errorNumber = exception.Number
    }));
}

return 0;
