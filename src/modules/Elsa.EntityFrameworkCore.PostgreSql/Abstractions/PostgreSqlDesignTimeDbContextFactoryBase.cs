using Elsa.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Elsa.EntityFrameworkCore.PostgreSql.Abstractions;

/// <summary>
/// A base class for implementing a design-time factory for creating a <see cref="DbContext"/> with a PostgreSQL database.
/// </summary>
/// <typeparam name="TDbContext">The type of the <see cref="DbContext"/>.</typeparam>
public abstract class PostgreSqlDesignTimeDbContextFactoryBase<TDbContext> : IDesignTimeDbContextFactory<TDbContext> where TDbContext : DbContext
{
    /// <inheritdoc />
    public TDbContext CreateDbContext(string[] args)
    {
        var connectionString = args.Any() ? args[0] : "Data Source=local";
        
        var builder = new DbContextOptionsBuilder<TDbContext>();
        builder.UseElsaPostgreSql(connectionString);

        return (TDbContext)Activator.CreateInstance(typeof(TDbContext), builder.Options)!;
    }
}