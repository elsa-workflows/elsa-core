using Elsa.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Elsa.EntityFrameworkCore.SqlServer.Abstractions;

/// <summary>
/// A base class for implementing a design-time factory for creating a <see cref="DbContext"/> with a SQL Server database.
/// </summary>
/// <typeparam name="TDbContext">The type of the <see cref="DbContext"/>.</typeparam>
public abstract class SqlServerDesignTimeDbContextFactoryBase<TDbContext> : IDesignTimeDbContextFactory<TDbContext> where TDbContext : DbContext
{
    /// <inheritdoc />
    public TDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<TDbContext>();
        var connectionString = args.Any() ? args[0] : "Data Source=local";

        builder.UseElsaSqlServer(connectionString);

        return (TDbContext)Activator.CreateInstance(typeof(TDbContext), builder.Options)!;
    }
}