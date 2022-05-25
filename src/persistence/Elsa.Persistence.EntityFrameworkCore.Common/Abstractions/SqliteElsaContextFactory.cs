using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Elsa.Persistence.EntityFrameworkCore.Common.Abstractions
{
    public abstract class SqliteDesignTimeDbContextFactory<TDbContext> : IDesignTimeDbContextFactory<TDbContext> where TDbContext : DbContext
    {
        public TDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<TDbContext>();
            var connectionString = args.Any() ? args[0] : "Data Source=elsa.sqlite.db;Cache=Shared";

            builder.UseSqlite(connectionString, db => db
                .MigrationsAssembly(GetType().Assembly.GetName().Name)
                .MigrationsHistoryTable(ElsaDbContextBase.MigrationsHistoryTable, ElsaDbContextBase.ElsaSchema));

            return (TDbContext)Activator.CreateInstance(typeof(TDbContext), builder.Options)!;
        }
    }
}