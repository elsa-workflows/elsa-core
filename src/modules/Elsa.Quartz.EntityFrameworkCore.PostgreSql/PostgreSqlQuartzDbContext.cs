using AppAny.Quartz.EntityFrameworkCore.Migrations;
using AppAny.Quartz.EntityFrameworkCore.Migrations.PostgreSQL;
using Elsa.EntityFrameworkCore.Abstractions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Quartz.EntityFrameworkCore.PostgreSql;

/// <summary>
/// Quartz DbContext for PostgreSQL.
/// </summary>
[UsedImplicitly]
public class PostgreSqlQuartzDbContext : DbContext
{
    /// <inheritdoc />
    public PostgreSqlQuartzDbContext(DbContextOptions<PostgreSqlQuartzDbContext> options) : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.AddQuartz(model => model.UsePostgreSql());
}

/// <summary>
/// Design-time factory for Quartz DbContext for PostgreSQL.
/// </summary>
[UsedImplicitly]
public class SqlServerQuartzDbContextFactory : DesignTimeDbContextFactoryBase<PostgreSqlQuartzDbContext>
{
    /// <inheritdoc />
    protected override void ConfigureBuilder(DbContextOptionsBuilder<PostgreSqlQuartzDbContext> builder, string connectionString) => builder.UseNpgsql(connectionString);
}