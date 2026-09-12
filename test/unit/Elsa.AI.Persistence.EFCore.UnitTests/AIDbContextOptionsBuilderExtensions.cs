using Elsa.AI.Persistence.EFCore.Sqlite;
using Elsa.Persistence.EFCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Elsa.AI.Persistence.EFCore.UnitTests;

internal static class AIDbContextOptionsBuilderExtensions
{
    public static DbContextOptionsBuilder<AIDbContext> UseSqliteAIMigrations(this DbContextOptionsBuilder<AIDbContext> builder, SqliteConnection connection)
    {
        builder.UseElsaDbContextOptions(null);
        return builder.UseSqlite(connection, db => db.MigrationsAssembly(typeof(AIDbContextFactory).Assembly.GetName().Name));
    }
}
