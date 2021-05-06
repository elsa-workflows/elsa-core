using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFramework.Sqlite
{
    public static class DbContextOptionsBuilderExtensions
    {
        public static DbContextOptionsBuilder UseSqlite(this DbContextOptionsBuilder builder) => builder.UseSqlite("Data Source=elsa.sqlite.db;Cache=Shared;", db => db.MigrationsAssembly(typeof(SqliteElsaContextFactory).Assembly.GetName().Name));
    }
}