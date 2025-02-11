using AppAny.Quartz.EntityFrameworkCore.Migrations;
using AppAny.Quartz.EntityFrameworkCore.Migrations.SqlServer;
using Elsa.EntityFrameworkCore.Abstractions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Quartz.EntityFrameworkCore.SqlServer;

/// <summary>
/// Quartz DbContext for SQL Server.
/// </summary>
[UsedImplicitly]
public class SqlServerQuartzDbContext : DbContext
{
    /// <inheritdoc />
    public SqlServerQuartzDbContext(DbContextOptions<SqlServerQuartzDbContext> options) : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.AddQuartz(model => model.UseSqlServer());
}

/// <summary>
/// Design-time factory for Quartz DbContext for SQL Server.
/// </summary>
[UsedImplicitly]
public class SqlServerQuartzDbContextFactory : DesignTimeDbContextFactoryBase<SqlServerQuartzDbContext>
{
    /// <inheritdoc />
    protected override void ConfigureBuilder(DbContextOptionsBuilder<SqlServerQuartzDbContext> builder, string connectionString) => builder.UseSqlServer(connectionString);
}