using Elsa.EntityFrameworkCore.Sqlite.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Elsa.EntityFrameworkCore.Sqlite.Abstractions;

public abstract class SqliteDesignTimeDbContextFactoryBase<TDbContext> : IDesignTimeDbContextFactory<TDbContext> where TDbContext : DbContext
{
    public TDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<TDbContext>();
        var connectionString = args.Any() ? args[0] : Constants.DefaultConnectionString;

        builder.UseElsaSqlite(connectionString);

        return (TDbContext)Activator.CreateInstance(typeof(TDbContext), builder.Options)!;
    }
}