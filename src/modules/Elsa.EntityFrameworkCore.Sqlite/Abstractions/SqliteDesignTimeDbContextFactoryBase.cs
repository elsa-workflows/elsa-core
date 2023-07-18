using Elsa.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Elsa.EntityFrameworkCore.Sqlite.Abstractions;

/// <summary>
/// A base class for implementing a design-time factory for creating a <see cref="DbContext"/> with a SQLite database.
/// </summary>
/// <typeparam name="TDbContext">The type of the <see cref="DbContext"/>.</typeparam>
public abstract class SqliteDesignTimeDbContextFactoryBase<TDbContext> : IDesignTimeDbContextFactory<TDbContext> where TDbContext : DbContext
{
    /// <inheritdoc />
    public TDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<TDbContext>();
        var connectionString = args.Any() ? args[0] : Constants.DefaultConnectionString;

        builder.UseElsaSqlite(connectionString);

        return (TDbContext)Activator.CreateInstance(typeof(TDbContext), builder.Options)!;
    }
}